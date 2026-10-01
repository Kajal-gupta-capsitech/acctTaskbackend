namespace BackendAcctTask.Models;

public class UpdateChartAccountRequest
{
    public string? AccountName { get; set; }

    public string? AccountTypeId { get; set; }

    public string? AccountGroup { get; set; }

    public bool? ForClients { get; set; }

    public bool? Archive { get; set; }
}