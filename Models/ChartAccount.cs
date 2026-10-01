using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackendAcctTask.Models;

public class ChartAccount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    // Foreign key -> AccountType
    [BsonRepresentation(BsonType.ObjectId)]
    public string AccountTypeId { get; set; } = string.Empty;

    public string AccountGroup { get; set; } = string.Empty;

    public bool ForClients { get; set; }

    public bool Archive { get; set; }
}