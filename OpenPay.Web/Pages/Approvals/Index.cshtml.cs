using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Approvals;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Approvals;

[Authorize(Roles = $"{nameof(UserRole.Manager)},{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IApprovalService _approvalService;

    public IndexModel(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    public IReadOnlyList<PendingApprovalListItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _approvalService.GetPendingApprovalsAsync();
    }
}