using System;
using System.Collections.Generic;
using System.Text;

namespace SensorFacebook.Application.Services.KeywordServices
{
    public sealed class KeywordCsvRow
    {
        public string? Text { get; set; }
        public int? CategoryId { get; set; }
        public int? Priority { get; set; }
        public string? Active { get; set; } // "true/false/1/0/yes/no"
        
        public string? Location { get; set; }   // "lat, lon"
        public decimal? LocationLat { get; set; }
        public decimal? LocationLon { get; set; }

        public int? RadiusKm { get; set; }
        public string? RadiusPolicy { get; set; } // auto|platform|fixed

        public string? SortBy { get; set; }       // relevance|distance_asc|date_desc|price_asc|price_desc
        public string? Conditions { get; set; }   // "new;like_new;good" (dùng ; hoặc ,)
        public string? ListedTime { get; set; }   // all|24h|7d|30d
        public string? Availability { get; set; } // available|sold
    }
}
