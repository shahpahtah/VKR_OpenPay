namespace OpenPay.Application.DTOs.Payments;

public class PaymentOrderImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int ErrorRows { get; set; }

    public List<PaymentOrderImportRowResultDto> Items { get; set; } = [];
}