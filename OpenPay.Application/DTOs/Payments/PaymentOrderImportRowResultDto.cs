namespace OpenPay.Application.DTOs.Payments;

public class PaymentOrderImportRowResultDto
{
    public int RowNumber { get; set; }
    public string? DocumentNumber { get; set; }
    public string? CounterpartyInn { get; set; }
    public string? AmountText { get; set; }
    public bool IsImported { get; set; }
    public string Message { get; set; } = string.Empty;
}