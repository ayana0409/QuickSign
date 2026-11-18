namespace QuickSign.Application.Contracts.DTOs.Documents;

public sealed class DashboardOverviewStatsDto
{
    public int TotalSignedDocuments { get; set; }
    public int TotalVerifications { get; set; }
    public int UniqueVerifications { get; set; }
    public int TamperedAlerts { get; set; }
    public int ActiveCertificates { get; set; }
    public int ActiveApiKeys { get; set; }
}
