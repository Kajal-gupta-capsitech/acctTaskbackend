namespace BackendAcctTask.Models;

public class UpdateTrialBalanceRequest
{
    public string? RefNo { get; set; }

    public TrialBalanceType? TrialBalanceType { get; set; }

    public string? AccountingPeriodId { get; set; }

    public string? ChartAccountId { get; set; }

    public decimal? Turnover { get; set; }

    public string? Description { get; set; }

    public JournalType? JournalType { get; set; }

    public string? JournalId { get; set; }

    public string? AccountReports { get; set; }

    public ImportMode? ImportMode { get; set; }

    public string? ImportFormat { get; set; }

    public string? FileName { get; set; }

    public TrialBalanceStatus? Status { get; set; }
}

