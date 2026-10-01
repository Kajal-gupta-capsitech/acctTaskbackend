namespace BackendAcctTask.Models;

public class ChartAccountResponse
{
    public string Id { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    // Foreign key
    public string AccountTypeId { get; set; } = string.Empty;

    // Foreign table/object
    public AccountType? AccountType { get; set; }

    public string AccountGroup { get; set; } = string.Empty;

    public bool ForClients { get; set; }

    public bool Archive { get; set; }
}