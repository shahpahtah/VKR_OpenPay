using FluentAssertions;
using OpenPay.Application.DTOs.Reports;
using OpenPay.Infrastructure.Services;
using Xunit;

namespace OpenPay.Tests;

public class ReportExportTests
{
    [Fact]
    public void ExportToCsv_should_return_non_empty_bytes()
    {
        var service = new ReportExportService();

        var report = new ReportOverviewDto
        {
            Items = new List<PaymentReportItemDto>
            {
                new()
                {
                    DocumentNumber = "PAY-001",
                    CreatedAt = DateTime.UtcNow,
                    PaymentDate = new DateTime(2026, 4, 12),
                    CounterpartyName = "ООО Альфа",
                    Amount = 1000,
                    Currency = "RUB",
                    Status = "Executed"
                }
            }
        };

        var bytes = service.ExportToCsv(report);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExportToExcel_should_return_non_empty_bytes()
    {
        var service = new ReportExportService();

        var report = new ReportOverviewDto
        {
            StatusSummary = new List<StatusSummaryDto>
            {
                new() { Status = "Executed", Count = 1, TotalAmount = 1000 }
            },
            Items = new List<PaymentReportItemDto>
            {
                new()
                {
                    DocumentNumber = "PAY-001",
                    CreatedAt = DateTime.UtcNow,
                    PaymentDate = new DateTime(2026, 4, 12),
                    CounterpartyName = "ООО Альфа",
                    Amount = 1000,
                    Currency = "RUB",
                    Status = "Executed"
                }
            }
        };

        var bytes = service.ExportToExcel(report);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }
}