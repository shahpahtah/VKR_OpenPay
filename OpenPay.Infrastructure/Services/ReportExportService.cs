using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using OpenPay.Application.DTOs.Reports;
using OpenPay.Application.Interfaces;

namespace OpenPay.Infrastructure.Services;

public class ReportExportService : IReportExportService
{
    public byte[] ExportToCsv(ReportOverviewDto report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Номер;Дата создания;Дата платежа;Контрагент;Сумма;Валюта;Статус");

        foreach (var item in report.Items)
        {
            sb.Append(Escape(item.DocumentNumber)).Append(';');
            sb.Append(item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")).Append(';');
            sb.Append(item.PaymentDate?.ToString("dd.MM.yyyy") ?? "").Append(';');
            sb.Append(Escape(item.CounterpartyName)).Append(';');
            sb.Append(item.Amount.ToString("F2", CultureInfo.InvariantCulture)).Append(';');
            sb.Append(Escape(item.Currency)).Append(';');
            sb.Append(Escape(item.Status)).AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportToExcel(ReportOverviewDto report)
    {
        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Сводка");
        var paymentsSheet = workbook.Worksheets.Add("Платежи");

        FillSummarySheet(summarySheet, report);
        FillPaymentsSheet(paymentsSheet, report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void FillSummarySheet(IXLWorksheet ws, ReportOverviewDto report)
    {
        ws.Cell("A1").Value = "Отчетность по платежам";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        ws.Cell("A3").Value = "Дата с";
        ws.Cell("B3").Value = report.DateFrom?.ToString("dd.MM.yyyy") ?? "-";

        ws.Cell("A4").Value = "Дата по";
        ws.Cell("B4").Value = report.DateTo?.ToString("dd.MM.yyyy") ?? "-";

        ws.Cell("A6").Value = "Показатель";
        ws.Cell("B6").Value = "Количество";
        ws.Cell("C6").Value = "Сумма";

        var range = ws.Range("A6:C6");
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;

        ws.Cell("A7").Value = "Всего платежей";
        ws.Cell("B7").Value = report.TotalPaymentsCount;
        ws.Cell("C7").Value = report.TotalPaymentsAmount;

        ws.Cell("A8").Value = "Исполнено";
        ws.Cell("B8").Value = report.ExecutedPaymentsCount;
        ws.Cell("C8").Value = report.ExecutedPaymentsAmount;

        ws.Cell("A9").Value = "На согласовании";
        ws.Cell("B9").Value = report.PendingApprovalCount;
        ws.Cell("C9").Value = report.PendingApprovalAmount;

        ws.Cell("A10").Value = "Ошибки";
        ws.Cell("B10").Value = report.ErrorPaymentsCount;
        ws.Cell("C10").Value = report.ErrorPaymentsAmount;

        ws.Cell("A12").Value = "Сводка по статусам";
        ws.Cell("A12").Style.Font.Bold = true;

        ws.Cell("A13").Value = "Статус";
        ws.Cell("B13").Value = "Количество";
        ws.Cell("C13").Value = "Сумма";

        var range2 = ws.Range("A13:C13");
        range2.Style.Font.Bold = true;
        range2.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 14;
        foreach (var item in report.StatusSummary)
        {
            ws.Cell(row, 1).Value = item.Status;
            ws.Cell(row, 2).Value = item.Count;
            ws.Cell(row, 3).Value = item.TotalAmount;
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void FillPaymentsSheet(IXLWorksheet ws, ReportOverviewDto report)
    {
        ws.Cell("A1").Value = "Номер";
        ws.Cell("B1").Value = "Дата создания";
        ws.Cell("C1").Value = "Дата платежа";
        ws.Cell("D1").Value = "Контрагент";
        ws.Cell("E1").Value = "Сумма";
        ws.Cell("F1").Value = "Валюта";
        ws.Cell("G1").Value = "Статус";

        var range3 = ws.Range("A1:G1");
        range3.Style.Font.Bold = true;
        range3.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var item in report.Items)
        {
            ws.Cell(row, 1).Value = item.DocumentNumber;
            ws.Cell(row, 2).Value = item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            ws.Cell(row, 3).Value = item.PaymentDate?.ToString("dd.MM.yyyy") ?? "-";
            ws.Cell(row, 4).Value = item.CounterpartyName;
            ws.Cell(row, 5).Value = item.Amount;
            ws.Cell(row, 6).Value = item.Currency;
            ws.Cell(row, 7).Value = item.Status;
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}