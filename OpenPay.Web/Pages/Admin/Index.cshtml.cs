using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenPay.Application.DTOs.Admin;
using OpenPay.Application.DTOs.Audit;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Web.Common;

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

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UserQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EventType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ObjectId { get; set; }

    public List<SelectListItem> EventTypeOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        LoadEventTypes();

        var filter = new AuditLogFilterDto
        {
            DateFrom = DateFrom,
            DateTo = DateTo,
            UserQuery = UserQuery,
            EventType = EventType,
            ObjectId = ObjectId
        };

        Items = await _auditLogService.GetAllAsync(filter);
    }

    private void LoadEventTypes()
    {
        EventTypeOptions = Enum.GetValues<AuditEventType>()
            .Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = UiText.AuditEventText(x.ToString())
            })
            .ToList();

        EventTypeOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "Все события"
        });
    }
}
