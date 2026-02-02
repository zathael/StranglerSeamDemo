using System.Net.Http.Json;

namespace StranglerSeamDemo.LegacyWinForms.Api;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<PagedResult<CaseDto>> GetCasesAsync(string? search, int page, int pageSize)
    {
        var url = $"cases?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}";
        var resp = await _http.GetAsync(url);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"GET failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }

        var result = await resp.Content.ReadFromJsonAsync<PagedResult<CaseDto>>();
        return result ?? new PagedResult<CaseDto>(new List<CaseDto>(), 0, page, pageSize);
    }

    public async Task<CaseDto> UpdateStatusAsync(int id, string status)
    {
        var resp = await _http.PutAsJsonAsync($"cases/{id}/status", new UpdateStatusRequest(status));

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"PUT failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }

        var updated = await resp.Content.ReadFromJsonAsync<CaseDto>();
        return updated ?? throw new InvalidOperationException("API returned empty response.");
    }
}
