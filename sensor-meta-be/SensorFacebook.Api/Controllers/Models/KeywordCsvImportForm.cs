using Microsoft.AspNetCore.Http;

namespace SensorFacebook.Api.Controllers.Models
{
    public sealed class KeywordCsvImportForm
    {
        public IFormFile? File { get; set; }
    }
}
