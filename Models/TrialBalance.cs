using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackendAcctTask.Models;

[BsonIgnoreExtraElements]
public class TrialBalance
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    // TB-01, TB-02, TB-03...
    public string RefNo { get; set; } = string.Empty;

    // AccountingPeriod reference.
    // Used for Statutory Trial Balance.
   
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PeriodId { get; set; }

    // Period dates used for both
    // Statutory and Management.
    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    // Statutory = 0
    // Management = 1
    [BsonElement("TrialBalanceType")]
    public TrialBalanceType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    // Manual = 0
    // Csv = 1
    public ImportType ImportType { get; set; }

    // Currently only Default = 0
    public CsvImportType CsvImportType { get; set; }

    // Unbalanced = 0
    // Balanced = 1
    public TrialBalanceStatus Status { get; set; }

    public bool IsLocked { get; set; }

    public decimal Turnover { get; set; }

    public decimal TotalDebit { get; set; }

    public decimal TotalCredit { get; set; }

    public decimal TotalProfitLoss { get; set; }

    // No separate AccountReport entity currently.
    public object? AccountReports { get; set; }

    // Local CSV path.
    public string? CsvFilePath { get; set; }

    // Local attachment path.
    public string? AttachmentFilePath { get; set; }

    // Actual Trial Balance accounts.
    public List<TrialBalanceItem> Items { get; set; } = new();
}


// ============================================================
// TRIAL BALANCE ITEM
// ============================================================

public class TrialBalanceItem
{
    // ChartAccount is resolved using this code.
    // We do NOT store ChartAccountId.
    public string AccountCode { get; set; } = string.Empty;

    // Stored from ChartAccount.Name.
    public string AccountName { get; set; } = string.Empty;

    public AccountNature Nature { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    // Optional note from CSV/manual editing.
    public string Note { get; set; } = string.Empty;
}


// ============================================================
// ENUMS
// ============================================================

public enum TrialBalanceType
{
    Statutory = 0,
    Management = 1
}

public enum ImportType
{
    Manual = 0,
    Csv = 1
}

public enum CsvImportType
{
    Default = 0
}

public enum TrialBalanceStatus
{
    Unbalanced = 0,
    Balanced = 1
}

public enum AccountNature
{
    Debit = 1,
    Credit = 2
}