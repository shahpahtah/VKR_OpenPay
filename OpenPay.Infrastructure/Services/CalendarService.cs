using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Calendar;
using OpenPay.Application.Interfaces;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public CalendarService(
        OpenPayDbContext dbContext,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _currentOrganizationService = currentOrganizationService;
    }

    public async Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(DateTime? from, DateTime? to)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var dateFrom = from?.Date ?? DateTime.Today;
        var dateToInclusive = to?.Date ?? DateTime.Today.AddDays(30);
        var dateToExclusive = dateToInclusive.AddDays(1);

        var payments = await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Counterparty)
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.PaymentDate.HasValue &&
                x.PaymentDate.Value >= dateFrom &&
                x.PaymentDate.Value < dateToExclusive)
            .OrderBy(x => x.PaymentDate)
            .ThenBy(x => x.DocumentNumber)
            .Select(x => new
            {
                PaymentDate = x.PaymentDate!.Value,
                Item = new CalendarPaymentItemDto
                {
                    Id = x.Id,
                    DocumentNumber = x.DocumentNumber,
                    CounterpartyName = x.Counterparty != null ? x.Counterparty.FullName : "-",
                    Amount = x.Amount,
                    Currency = x.Currency,
                    Status = x.Status.ToString()
                }
            })
            .ToListAsync();

        return payments
            .GroupBy(x => x.PaymentDate.Date)
            .Select(g => new CalendarDayDto
            {
                Date = g.Key,
                Payments = g.Select(x => x.Item).ToList()
            })
            .OrderBy(x => x.Date)
            .ToList();
    }
}