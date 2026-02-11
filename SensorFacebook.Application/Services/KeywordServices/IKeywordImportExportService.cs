using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.KeywordServices
{
    public interface IKeywordImportExportService
    {
        Task<Stream> ExportCsvAsync(
            string? q, int? categoryId, bool? active,
            string? sortBy, IEnumerable<string>? conditions, string? listedTime, string? availability,
            CancellationToken ct = default);

        Task<KeywordImportResult> ImportCsvAsync(Stream csvStream, CancellationToken ct = default);
    }

    public sealed class KeywordImportResult
    {
        public int Total { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }
        public List<KeywordImportError> Errors { get; set; } = new();
    }

    public sealed class KeywordImportError
    {
        public int RowNumber { get; set; }
        public string Error { get; set; } = "";
        public string? Raw { get; set; }
    }
}
