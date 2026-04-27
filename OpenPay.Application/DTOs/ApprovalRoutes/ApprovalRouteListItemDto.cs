using OpenPay.Domain.Enums;

namespace OpenPay.Application.DTOs.ApprovalRoutes;

public class ApprovalRouteListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? ExpenseType { get; set; }
    public string? Department { get; set; }
    public ApprovalType ApprovalType { get; set; }
    public bool IsActive { get; set; }
}
