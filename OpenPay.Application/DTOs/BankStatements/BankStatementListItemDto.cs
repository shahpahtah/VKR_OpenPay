namespace OpenPay.Application.DTOs.BankStatements;

public class BankStatementListItemDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AccountDisplay { get; set; } = string.Empty;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public int TotalOperations { get; set; }
    public int MatchedOperations { get; set; }
    public int UnmatchedOperations { get; set; }
}
