namespace OpenPay.Application.DTOs.Counterparties;

public class CounterpartyImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int ErrorRows { get; set; }

    public List<CounterpartyImportRowResultDto> Items { get; set; } = [];
}