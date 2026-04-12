using OpenPay.Application.DTOs.Approvals;

namespace OpenPay.Application.Interfaces;

public interface IApprovalService
{
    Task SubmitForApprovalAsync(Guid paymentOrderId);
    Task<IReadOnlyList<PendingApprovalListItemDto>> GetPendingApprovalsAsync();
    Task<ApprovalReviewDto?> GetReviewModelAsync(Guid paymentOrderId);

    Task ApproveAsync(Guid paymentOrderId, string approverUserId, string? comment);
    Task RejectAsync(Guid paymentOrderId, string approverUserId, string comment);
    Task ReturnForReworkAsync(Guid paymentOrderId, string approverUserId, string comment);
    Task<IReadOnlyList<ApprovalDecisionHistoryItemDto>> GetHistoryAsync(Guid paymentOrderId);
}