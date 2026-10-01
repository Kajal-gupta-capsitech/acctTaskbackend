namespace BackendAcctTask.Models;

public class UpdateAccountingPeriodRequest
{
    public DateTime? PeriodFrom { get; set; }

    public DateTime? PeriodTo { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsClosed { get; set; }
}