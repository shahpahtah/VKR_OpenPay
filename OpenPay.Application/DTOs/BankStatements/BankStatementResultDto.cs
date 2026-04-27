namespace OpenPay.Application.DTOs.BankStatements;

public class BankStatementResultDto
{
    public Guid BankStatementId { get; set; }
    public int TotalOperations { get; set; }
    public int MatchedOperations { get; set; }
    public int UnmatchedOperations { get; set; }
    public IReadOnlyList<BankStatementReconciliationItemDto> Items { get; set; } = [];
}
