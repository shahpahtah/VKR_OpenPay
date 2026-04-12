namespace OpenPay.Domain.Enums;

public enum AuditEventType
{
    PaymentCreated = 1,
    PaymentUpdated = 2,
    PaymentApproved = 3,
    PaymentRejected = 4,
    PaymentSentToBank = 5,
    CounterpartyCreated = 6,
    BankStatementImported = 7,
    RouteChanged = 8,
    UserLogin = 9,

    PaymentSubmittedForApproval = 10,
    PaymentReturnedForRework = 11,
    PaymentExecutedByBank = 12,
    PaymentBankError = 13
}