namespace StranglerSeamDemo.LegacyWinForms.Api;

public record CaseDto(int Id, string PatientName, string Procedure, string Status, DateTime LastUpdatedUtc);

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public record UpdateStatusRequest(string Status);
