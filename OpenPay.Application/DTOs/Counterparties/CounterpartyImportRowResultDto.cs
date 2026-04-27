namespace OpenPay.Application.DTOs.Counterparties;

public class CounterpartyImportRowResultDto
{
    public int RowNumber { get; set; }
    public string? Inn { get; set; }
    public string? FullName { get; set; }
    public bool IsImported { get; set; }
    public string Message { get; set; } = string.Empty;
}