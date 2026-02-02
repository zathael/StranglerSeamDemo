namespace StranglerSeamDemo.LegacyWinForms.Api;

public interface ICasesGateway
{
    Task<PagedResult<CaseDto>> GetCasesAsync(string? search, int page, int pageSize);
    Task<CaseDto> UpdateStatusAsync(int id, string status);
}
