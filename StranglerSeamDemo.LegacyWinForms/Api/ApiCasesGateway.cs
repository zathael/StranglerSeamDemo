namespace StranglerSeamDemo.LegacyWinForms.Api;

public sealed class ApiCasesGateway : ICasesGateway
{
    private readonly ApiClient _client;

    public ApiCasesGateway(string baseUrl) => _client = new ApiClient(baseUrl);

    public Task<PagedResult<CaseDto>> GetCasesAsync(string? search, int page, int pageSize)
        => _client.GetCasesAsync(search, page, pageSize);

    public Task<CaseDto> UpdateStatusAsync(int id, string status)
        => _client.UpdateStatusAsync(id, status);
}
