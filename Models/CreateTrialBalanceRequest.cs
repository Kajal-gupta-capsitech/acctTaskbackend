using Microsoft.AspNetCore.Http;

namespace BackendAcctTask.Models;

public class CreateTrialBalanceRequest
{
    public TrialBalanceType TrialBalanceType { get; set; }

    public string? AccountingPeriodId { get; set; }

    public string? ChartAccountId { get; set; }

    public ImportMode ImportMode { get; set; }

    public string? ImportFormat { get; set; }

    public IFormFile? File { get; set; }
}