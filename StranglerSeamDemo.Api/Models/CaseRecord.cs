namespace StranglerSeamDemo.Api.Models;

public class CaseRecord
{
    public int Id { get; set; }
    public string PatientName { get; set; } = "";
    public string Procedure { get; set; } = "";
    public string Status { get; set; } = "New";
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
