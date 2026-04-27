namespace OpenPay.Web.Common;

public static class UiText
{
    public static string PaymentStatusCss(string status) => status switch
    {
        "Draft" => "badge-soft badge-draft",
        "PendingApproval" => "badge-soft badge-pending",
        "Approved" => "badge-soft badge-approved",
        "Executed" => "badge-soft badge-executed",
        "Rejected" => "badge-soft badge-rejected",
        "Error" => "badge-soft badge-error",
        "Sent" => "badge-soft badge-approved",
        "ReadyToSend" => "badge-soft badge-approved",
        "Rework" => "badge-soft badge-pending",
        _ => "badge-soft badge-draft"
    };

    public static string PaymentStatusText(string status) => status switch
    {
        "Draft" => "Черновик",
        "PendingApproval" => "На согласовании",
        "Approved" => "Согласован",
        "Rejected" => "Отклонен",
        "Rework" => "На доработке",
        "ReadyToSend" => "Готов к отправке",
        "Sent" => "Отправлен в банк",
        "Executed" => "Исполнен",
        "Error" => "Ошибка",
        _ => status
    };

    public static string RoleText(string role) => role switch
    {
        "Administrator" => "Администратор",
        "Accountant" => "Бухгалтер",
        "Manager" => "Руководитель",
        "PlatformAdmin" => "Платформенный администратор",
        _ => role
    };

    public static string AuditEventCss(string eventType) => eventType switch
    {
        "PaymentCreated" => "badge-soft badge-approved",
        "PaymentUpdated" => "badge-soft badge-approved",
        "PaymentSubmittedForApproval" => "badge-soft badge-pending",
        "PaymentApproved" => "badge-soft badge-executed",
        "PaymentRejected" => "badge-soft badge-rejected",
        "PaymentReturnedForRework" => "badge-soft badge-pending",
        "PaymentSentToBank" => "badge-soft badge-approved",
        "PaymentExecutedByBank" => "badge-soft badge-executed",
        "PaymentBankError" => "badge-soft badge-error",
        "PaymentSigned" => "badge-soft badge-approved",
        "CounterpartyCreated" => "badge-soft badge-executed",
        "CounterpartyUpdated" => "badge-soft badge-approved",
        "CounterpartyDeactivated" => "badge-soft badge-draft",
        "BankAccountCreated" => "badge-soft badge-executed",
        "BankAccountUpdated" => "badge-soft badge-approved",
        "BankAccountDeactivated" => "badge-soft badge-draft",
        "ApprovalRouteCreated" => "badge-soft badge-executed",
        "ApprovalRouteUpdated" => "badge-soft badge-approved",
        "ApprovalRouteDeactivated" => "badge-soft badge-draft",
        "BankConnectionCreated" => "badge-soft badge-executed",
        "BankConnectionUpdated" => "badge-soft badge-approved",
        "BankConnectionDeactivated" => "badge-soft badge-draft",
        "BankStatementImported" => "badge-soft badge-approved",
        "BankStatementReconciled" => "badge-soft badge-executed",
        _ => "badge-soft badge-draft"
    };

    public static string AuditEventText(string eventType) => eventType switch
    {
        "PaymentCreated" => "Платеж создан",
        "PaymentUpdated" => "Платеж обновлен",
        "PaymentSubmittedForApproval" => "Отправлен на согласование",
        "PaymentApproved" => "Платеж согласован",
        "PaymentRejected" => "Платеж отклонен",
        "PaymentReturnedForRework" => "Возврат на доработку",
        "PaymentSentToBank" => "Отправлен в банк",
        "PaymentExecutedByBank" => "Исполнен банком",
        "PaymentBankError" => "Ошибка банка",
        "PaymentSigned" => "Платеж подписан",
        "CounterpartyCreated" => "Контрагент создан",
        "CounterpartyUpdated" => "Контрагент обновлен",
        "CounterpartyDeactivated" => "Контрагент деактивирован",
        "BankAccountCreated" => "Счет создан",
        "BankAccountUpdated" => "Счет обновлен",
        "BankAccountDeactivated" => "Счет деактивирован",
        "ApprovalRouteCreated" => "Маршрут создан",
        "ApprovalRouteUpdated" => "Маршрут обновлен",
        "ApprovalRouteDeactivated" => "Маршрут отключен",
        "BankConnectionCreated" => "Банк подключен",
        "BankConnectionUpdated" => "Подключение обновлено",
        "BankConnectionDeactivated" => "Подключение отключено",
        "BankStatementImported" => "Выписка загружена",
        "BankStatementReconciled" => "Выписка сверена",
        _ => eventType
    };

    public static string ApprovalTypeText(string approvalType) => approvalType switch
    {
        "Sequential" => "Последовательное",
        "Parallel" => "Параллельное",
        _ => approvalType
    };

    public static string ApprovalDecisionText(string decision) => decision switch
    {
        "Approved" => "Утверждено",
        "Rejected" => "Отклонено",
        "Rework" => "На доработку",
        _ => decision
    };
}
