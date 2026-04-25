using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Reports;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public ReportService(
        OpenPayDbContext dbContext,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _currentOrganizationService = currentOrganizationService;
    }

    public async Task<ReportOverviewDto> GetOverviewAsync(DateTime? dateFrom, DateTime? dateTo)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var query = _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Counterparty)
            .Where(x => x.OrganizationId == organizationId)
            .AsQueryable();

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            query = query.Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value >= from);
        }

        if (dateTo.HasValue)
        {
            var toExclusive = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value < toExclusive);
        }

        var items = await query
            .OrderByDescending(x => x.PaymentDate ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PaymentReportItemDto
            {
                Id = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedAt = x.CreatedAt,
                PaymentDate = x.PaymentDate,
                CounterpartyName = x.Counterparty != null ? x.Counterparty.FullName : "-",
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status.ToString()
            })
            .ToListAsync();

        var statusSummary = items
            .GroupBy(x => x.Status)
            .Select(g => new StatusSummaryDto
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Status)
            .ToList();

        var executedItems = items.Where(x => x.Status == PaymentStatus.Executed.ToString()).ToList();
        var pendingApprovalItems = items.Where(x => x.Status == PaymentStatus.PendingApproval.ToString()).ToList();
        var errorItems = items.Where(x => x.Status == PaymentStatus.Error.ToString()).ToList();

        return new ReportOverviewDto
        {
            DateFrom = dateFrom,
            DateTo = dateTo,

            TotalPaymentsCount = items.Count,
            TotalPaymentsAmount = items.Sum(x => x.Amount),

            ExecutedPaymentsCount = executedItems.Count,
            ExecutedPaymentsAmount = executedItems.Sum(x => x.Amount),

            PendingApprovalCount = pendingApprovalItems.Count,
            PendingApprovalAmount = pendingApprovalItems.Sum(x => x.Amount),

            ErrorPaymentsCount = errorItems.Count,
            ErrorPaymentsAmount = errorItems.Sum(x => x.Amount),

            StatusSummary = statusSummary,
            Items = items
        };
    }
}