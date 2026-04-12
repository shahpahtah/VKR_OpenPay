using OpenPay.Application.DTOs.Calendar;
using static System.Net.Mime.MediaTypeNames;
namespace OpenPay.Application.DTOs.Calendar;

public class CalendarDayDto
{
    public DateTime Date { get; set; }
    public List<CalendarPaymentItemDto> Payments { get; set; } = [];
}