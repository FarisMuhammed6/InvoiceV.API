using InvoiceV.API.DataContext;
using InvoiceV.API.Entites;
using InvoiceV.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace InvoiceV.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceUploadController : ControllerBase
    {
        private readonly OcrService _ocrService;
        private readonly InvoiceVerContext _dbcontext;
        public InvoiceUploadController(OcrService ocrService, InvoiceVerContext dbcontext)
        {
            _ocrService = ocrService;
            _dbcontext = dbcontext;
        }
        [HttpPost("invoice")]
        public async Task<ActionResult> UploadInvoiceImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Invalid file.");
            }

            var filePath = Path.Combine("uploads", file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var extractedText = _ocrService.ExtractTextFromImage(filePath);
            var clientName = ExtractClientFromText(extractedText);
            var invoiceDate = ExtractInvoiceDate(extractedText);
            var amount = ExtractAmountFromText(extractedText);

            var invoice = new Invoice
            {
                ClientName = clientName,
                InvoiceDate = invoiceDate,
                Amount = amount,
                IsValid = ValidateInvoice(clientName, invoiceDate, amount)
            };

            _dbcontext.Invoices.Add(invoice);
            await _dbcontext.SaveChangesAsync();
            return Ok(invoice);
        }
        private string ExtractClientFromText(string text)
        {
            var clientPattern = @"Client:\s*(\w+)";
            var match = Regex.Match(text, clientPattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            throw new Exception("Client name not found.");
        }
        private DateTime ExtractInvoiceDate(string text)
        {
            var datePattern = @"\b\d{1,2}/\d{1,2}/\d{4}\b";
            var match = Regex.Match(text, datePattern);

            if (match.Success)
            {
                if (DateTime.TryParse(match.Value, out DateTime date))
                {
                    return date;
                }
            }
            throw new Exception("Date not found or invalid date format.");
        }
        private decimal ExtractAmountFromText(string text)
        {
            var amountPattern = @"(?:total amount due|amount|cost|price|fee|charge|payment).*?(\$?\d+(\.\d{1,2})?)";

            var match = Regex.Match(text, amountPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var amountString = match.Groups[1].Value;
                amountString = amountString.Replace("$", "");

                if (decimal.TryParse(amountString, out decimal amount))
                {
                    return amount;
                }
            }
            throw new Exception("Total amount not found or invalid amount format.");
        }
        private bool ValidateInvoice(string clientName, DateTime invoiceDate, decimal amount)
        {
            var contract = _dbcontext.contracts
                .FirstOrDefault(c => c.ClientName == clientName &&
                                     c.StartDate <= invoiceDate &&
                                     c.EndDate >= invoiceDate &&
                                     c.Amount >= amount);

            return contract != null;
        }
    }
}
