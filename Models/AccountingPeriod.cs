using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackendAcctTask.Models;

public class AccountingPeriod
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public DateTime PeriodFrom { get; set; }

    public DateTime PeriodTo { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsClosed { get; set; } = false;
}