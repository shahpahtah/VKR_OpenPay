using OpenPay.Application.DTOs.Calendar;

namespace OpenPay.Application.Interfaces;

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(DateTime? from, DateTime? to);
}