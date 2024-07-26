using InvoiceV.API.DataContext;
using InvoiceV.API.Entites;
using InvoiceV.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractUploadController : ControllerBase
    {
        private readonly OcrService _ocrService;
        private readonly InvoiceVerContext _dbcontext;
        public ContractUploadController(OcrService ocrService, InvoiceVerContext dbcontext)
        {
            _ocrService = ocrService;
            _dbcontext = dbcontext;
        }
        [HttpPost("contract")]
        public async Task<ActionResult> UploadContractImage(IFormFile file)
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
            var clientnNAME = ExtractClientFromText(extractedText);
            (DateTime startDate, DateTime endDate) = ExtractContractDates(extractedText);
            var amount = ExtractAmountFromText(extractedText);

            var contract = new Contract
            {
                ClientName = clientnNAME,
                StartDate = startDate,
                EndDate = endDate,
                Amount = amount
            };
            _dbcontext.contracts.Add(contract);
            await _dbcontext.SaveChangesAsync();
            return Ok(contract);
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
        private static (DateTime startDate, DateTime endDate) ExtractContractDates(string text)
        {
            var datePattern = @"\b\d{1,2}/\d{1,2}/\d{4}\b";
            var matches = Regex.Matches(text, datePattern);

            // Check if there are at least two dates
            if (matches.Count >= 2)
            {
                // Dates are: [start date, ...other dates..., end date]
                // Assuming start date is the first occurrence and end date is the last occurrence
                if (DateTime.TryParse(matches[0].Value, out DateTime startDate) &&
                    DateTime.TryParse(matches[matches.Count - 1].Value, out DateTime endDate))
                {
                    return (startDate, endDate);
                }
            }
            throw new Exception("Start date or end date not found or invalid date format.");
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
    }
}










