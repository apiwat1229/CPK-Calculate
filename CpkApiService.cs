using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CPK_Calculate
{
    public class CpkAnalysisSummary
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public class CpkAnalysisDetail
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public double Lsl { get; set; }
        public double Usl { get; set; }
        public int SubgroupSize { get; set; }
        public List<double> DataPoints { get; set; } = new();
        public string Note { get; set; } = "";
        public string RecordedBy { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
    }

    public class CpkAnalysisCreateRequest
    {
        public string Title { get; set; } = "";
        public double Lsl { get; set; }
        public double Usl { get; set; }
        public int SubgroupSize { get; set; }
        public List<double> DataPoints { get; set; } = new();
        public string Note { get; set; } = "";
        public string RecordedBy { get; set; } = "";
    }

    public static class CpkApiService
    {
        private static readonly HttpClient _http = new()
        {
            BaseAddress = new Uri("https://app.ytrc.co.th/api/"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task<List<CpkAnalysisSummary>> GetAllAsync()
        {
            var result = await _http.GetFromJsonAsync<List<CpkAnalysisSummary>>("cpk-analyses");
            return result ?? new();
        }

        public static async Task<CpkAnalysisDetail?> GetByIdAsync(string id)
        {
            return await _http.GetFromJsonAsync<CpkAnalysisDetail>($"cpk-analyses/{id}");
        }

        public static async Task DeleteAsync(string id)
        {
            await _http.DeleteAsync($"cpk-analyses/{id}");
        }

        public static async Task<CpkAnalysisDetail?> CreateAsync(CpkAnalysisCreateRequest request)
        {
            var response = await _http.PostAsJsonAsync("cpk-analyses", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CpkAnalysisDetail>();
        }
    }
}
