namespace OpenPay.Domain.Enums;

public enum PaymentStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Rework = 5,
    ReadyToSend = 6,
    Sent = 7,
    Executed = 8,
    Error = 9
}