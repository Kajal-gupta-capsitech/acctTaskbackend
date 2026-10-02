using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackendAcctTask.Models;

public class TrialBalanceImport
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TrialBalanceId { get; set; } = string.Empty;

    public List<ImportColumn> Columns { get; set; } = new();

    public List<string> Headers { get; set; } = new();

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public List<TrialBalanceImportRow> Rows { get; set; } = new();

    public ImportType ImportType { get; set; } = ImportType.Csv;

    public CsvImportType CsvImportType { get; set; } = CsvImportType.Default;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class ImportColumn
{
    public int Type { get; set; }

    public int Index { get; set; }

    public string Name { get; set; } = string.Empty;
}


public class TrialBalanceImportRow
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public AccountNature Nature { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public string Note { get; set; } = string.Empty;
}
