using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Audit;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages.Admin;

[Authorize(Roles = $"{nameof(UserRole.Administrator)}")]
public class IndexModel : PageModel
{
    private readonly IAuditLogService _auditLogService;

    public IndexModel(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public IReadOnlyList<AuditLogListItemDto> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _auditLogService.GetRecentAsync(200);
    }
}