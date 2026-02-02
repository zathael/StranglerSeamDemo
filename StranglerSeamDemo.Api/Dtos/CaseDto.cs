namespace StranglerSeamDemo.Api.Dtos;

public record CaseDto(
    int Id,
    string PatientName,
    string Procedure,
    string Status,
    DateTime LastUpdatedUtc
);
