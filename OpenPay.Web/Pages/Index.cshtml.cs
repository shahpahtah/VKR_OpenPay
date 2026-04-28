using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenPay.Application.DTOs.Approvals;
using OpenPay.Application.DTOs.Payments;
using OpenPay.Application.DTOs.Reports;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;

namespace OpenPay.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IReportService _reportService;
    private readonly IPaymentOrderService _paymentOrderService;
    private readonly IApprovalService _approvalService;
    private readonly IOrganizationManagementService _organizationManagementService;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public IndexModel(
        IReportService reportService,
        IPaymentOrderService paymentOrderService,
        IApprovalService approvalService,
        IOrganizationManagementService organizationManagementService,
        ICurrentOrganizationService currentOrganizationService)
    {
        _reportService = reportService;
        _paymentOrderService = paymentOrderService;
        _approvalService = approvalService;
        _organizationManagementService = organizationManagementService;
        _currentOrganizationService = currentOrganizationService;
    }

    public bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;
    public bool IsAccountant { get; private set; }
    public bool IsManager { get; private set; }
    public bool IsAdministrator { get; private set; }
    public bool IsPlatformAdmin { get; private set; }

    public bool CanSeeOperations => IsAccountant || IsAdministrator;
    public bool CanSeeApprovals => IsManager || IsAdministrator;

    public ReportOverviewDto? Report { get; private set; }
    public IReadOnlyList<PaymentOrderListItemDto> RecentPayments { get; private set; } = [];
    public IReadOnlyList<PendingApprovalListItemDto> PendingApprovals { get; private set; } = [];
    public int OrganizationCount { get; private set; }
    public string? CurrentOrganizationName { get; private set; }
    public string? CurrentOrganizationInn { get; private set; }

    public async Task OnGetAsync()
    {
        if (!IsAuthenticated)
            return;

        IsAccountant = User.IsInRole(nameof(UserRole.Accountant));
        IsManager = User.IsInRole(nameof(UserRole.Manager));
        IsAdministrator = User.IsInRole(nameof(UserRole.Administrator));
        IsPlatformAdmin = User.IsInRole(nameof(UserRole.PlatformAdmin));

        if (!IsPlatformAdmin)
        {
            var organization = await _currentOrganizationService.GetCurrentOrganizationInfoAsync();
            CurrentOrganizationName = organization?.Name;
            CurrentOrganizationInn = organization?.Inn;
        }

        if (CanSeeOperations)
        {
            Report = await _reportService.GetOverviewAsync(null, null);

            var payments = await _paymentOrderService.GetAllAsync(null);
            RecentPayments = payments.Take(5).ToList();
        }

        if (CanSeeApprovals)
        {
            var pending = await _approvalService.GetPendingApprovalsAsync();
            PendingApprovals = pending.Take(5).ToList();
        }

        if (IsPlatformAdmin)
        {
            var organizations = await _organizationManagementService.GetAllAsync();
            OrganizationCount = organizations.Count;
        }
    }
}
