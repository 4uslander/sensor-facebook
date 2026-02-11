using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorFacebook.Api.Controllers.Models;
using SensorFacebook.Application.Services.KeywordServices;
using System.Text;

namespace SensorFacebook.Api.Controllers
{
    [ApiController]
    [Route("api/keywords/csv")]
    public sealed class KeywordImportExportController : ControllerBase
    {
        private readonly IKeywordImportExportService _svc;
        public KeywordImportExportController(IKeywordImportExportService svc) => _svc = svc;

        // GET /api/keywords/csv/export
        [HttpGet("export")]
        //[Authorize(Roles = "admin")]
        public async Task<IActionResult> Export(
            [FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] bool? active,
            [FromQuery] string? sortBy, [FromQuery(Name = "conditions")] string[]? conditions,
            [FromQuery] string? listedTime, [FromQuery] string? availability,
            CancellationToken ct)
        {
            var stream = await _svc.ExportCsvAsync(q, categoryId, active, sortBy, conditions, listedTime, availability, ct);
            var fileName = $"keywords_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(stream, "text/csv; charset=utf-8", fileName);
        }

        // POST /api/keywords/csv/import  (form-data: file=@keywords.csv)
        [HttpPost("import")]
        //[Authorize(Roles = "admin")]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
        public async Task<IActionResult> Import([FromForm] KeywordCsvImportForm form, CancellationToken ct)
        {
            if (form.File is null || form.File.Length == 0)
                return BadRequest(new { error = "CSV file is required" });

            await using var stream = form.File.OpenReadStream();
            var report = await _svc.ImportCsvAsync(stream, ct);
            return Ok(report);
        }

        // GET sample
        [HttpGet("sample")]
        [AllowAnonymous]
        public IActionResult Sample()
        {
            const string sample =
                "Text,CategoryId,Priority,Active,Location,LocationLat,LocationLon,RadiusKm,RadiusPolicy,SortBy,Conditions,ListedTime,Availability\r\n" +
                "karaoke amplifier,1,1,true,\"10.83566274, 106.77824450\",,,80,platform,relevance,\"new;good\",7d,available\r\n" +
                "denon receiver,2,2,1,,10.83566274,106.77824450,50,auto,date_desc,like_new,24h,sold\r\n" +
                "jbl speaker,,1,true,,, ,150,fixed,price_asc,\"new;fair\",all,available\r\n";
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sample);
            return File(bytes, "text/csv; charset=utf-8", "keywords_sample.csv");
        }
    }
}
