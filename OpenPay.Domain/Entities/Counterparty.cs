namespace OpenPay.Domain.Entities;

public class Counterparty : BaseEntity
{
    public string Inn { get; set; } = string.Empty;
    public string Kpp { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string CorrespondentAccount { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
}