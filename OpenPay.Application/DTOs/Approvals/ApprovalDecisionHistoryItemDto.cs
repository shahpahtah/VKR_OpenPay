namespace OpenPay.Application.DTOs.Approvals;

public class ApprovalDecisionHistoryItemDto
{
    public DateTime CreatedAt { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Comment { get; set; }
}