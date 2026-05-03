# Архитектура OpenPay

OpenPay - прототип системы управления платежными поручениями организации. Архитектура построена слоями: доменная модель, прикладные контракты, инфраструктурные сервисы и Razor Pages UI.

## Цели архитектуры

- Отделить бизнес-логику от UI.
- Оставить расширяемую точку интеграции с банками через адаптеры.
- Хранить токены банковских подключений в защищенном виде.
- Поддержать маршруты согласования, аудит, отчеты, выписки и сверку.
- Сохранить простую демонстрационную эксплуатацию без реальных банковских сертификатов и криптоподписи.

## Слои решения

| Проект | Назначение |
|---|---|
| `OpenPay.Domain` | Сущности и enum: организации, пользователи, счета, контрагенты, платежи, маршруты, выписки, аудит |
| `OpenPay.Application` | DTO, интерфейсы сервисов, валидаторы реквизитов |
| `OpenPay.Infrastructure` | EF Core, SQLite, Identity, реализации сервисов, банковские адаптеры, Data Protection |
| `OpenPay.Web` | Razor Pages UI, авторизация, формы, навигация, CSS/JS |
| `OpenPay.Tests` | Unit/service/web smoke тесты на xUnit |

## Компонентная схема

```mermaid
flowchart LR
    User["Пользователь"] --> Web["OpenPay.Web<br/>Razor Pages"]
    Web --> Identity["ASP.NET Identity"]
    Web --> AppContracts["OpenPay.Application<br/>DTO + interfaces"]
    AppContracts --> Infra["OpenPay.Infrastructure<br/>services"]
    Infra --> Db["SQLite / EF Core"]
    Infra --> Protection["ASP.NET Data Protection"]
    Infra --> Registry["IBankAdapterRegistry"]
    Registry --> MockBank["MockBankAdapter"]
    Registry --> TBankSandbox["TBankSandboxAdapter"]
    TBankSandbox --> TBankApi["T-Банк Sandbox API"]
```

## Основные сущности

```mermaid
classDiagram
    class Organization {
        Guid Id
        string Name
        string Inn
        string Kpp
        bool IsActive
    }

    class ApplicationUser {
        string Id
        string FullName
        UserRole Role
        Guid OrganizationId
        bool IsActive
    }

    class Counterparty {
        Guid Id
        Guid OrganizationId
        string FullName
        string Inn
        string Kpp
        string Bic
        string AccountNumber
        string CorrespondentAccount
        bool IsActive
    }

    class OrganizationBankAccount {
        Guid Id
        Guid OrganizationId
        Guid BankConnectionId
        string Bic
        string AccountNumber
        string BankName
        string Currency
        string ResponsibleUnit
        bool IsActive
    }

    class BankConnection {
        Guid Id
        Guid OrganizationId
        string BankCode
        string DisplayName
        string ProtectedAccessToken
        string ProtectedRefreshToken
        bool IsActive
    }

    class PaymentOrder {
        Guid Id
        Guid OrganizationId
        Guid CounterpartyId
        Guid OrganizationBankAccountId
        Guid ApprovalRouteId
        string DocumentNumber
        decimal Amount
        string Currency
        string ExpenseType
        string Purpose
        PaymentStatus Status
        DateTime SignedAt
        string SignatureReference
        string BankReferenceId
    }

    class ApprovalRoute {
        Guid Id
        Guid OrganizationId
        string Name
        decimal MinAmount
        decimal MaxAmount
        string ExpenseType
        string Department
        ApprovalType ApprovalType
        bool IsActive
    }

    class ApprovalDecision {
        Guid Id
        Guid PaymentOrderId
        string ApproverUserId
        ApprovalDecisionType Decision
        string Comment
    }

    class BankStatement {
        Guid Id
        Guid OrganizationId
        Guid OrganizationBankAccountId
        DateOnly PeriodFrom
        DateOnly PeriodTo
        string RawDataJson
        int TotalOperations
        int MatchedOperations
        int UnmatchedOperations
    }

    class AuditLogEntry {
        Guid Id
        Guid OrganizationId
        AuditEventType EventType
        string UserId
        string Description
        string EntityId
    }

    Organization "1" --> "*" ApplicationUser
    Organization "1" --> "*" Counterparty
    Organization "1" --> "*" OrganizationBankAccount
    Organization "1" --> "*" BankConnection
    Organization "1" --> "*" PaymentOrder
    Organization "1" --> "*" ApprovalRoute
    OrganizationBankAccount "*" --> "1" BankConnection
    PaymentOrder "*" --> "1" Counterparty
    PaymentOrder "*" --> "1" OrganizationBankAccount
    PaymentOrder "*" --> "0..1" ApprovalRoute
    PaymentOrder "1" --> "*" ApprovalDecision
    OrganizationBankAccount "1" --> "*" BankStatement
```

## Ключевые сервисы

| Сервис | Ответственность |
|---|---|
| `PaymentOrderService` | Создание, редактирование, поиск дублей, выбор маршрута, импорт платежей |
| `ApprovalService` | Отправка на согласование, утверждение, отклонение, возврат на доработку |
| `BankProcessingService` | Демо-подпись, отправка в банк, проверка банковского статуса |
| `BankGatewayService` | Выбор банковского адаптера по подключению счета |
| `BankConnectionService` | CRUD банковских подключений и защищенное хранение токенов |
| `BankStatementService` | Загрузка выписок, хранение raw JSON, сверка операций |
| `ApprovalRouteService` | CRUD маршрутов и условия согласования |
| `ReportService` | Агрегация отчетов по статусам и контрагентам |
| `ReportExportService` | CSV/XLSX экспорт |
| `AuditLogService` | Запись действий пользователя |
| `CurrentOrganizationService` | Изоляция данных по организации текущего пользователя |

## Банковские адаптеры

```mermaid
classDiagram
    class IBankAdapter {
        string BankCode
        string DisplayName
        SubmitPaymentAsync()
        CheckPaymentStatusAsync()
        LoadStatementAsync()
    }

    class IBankAdapterRegistry {
        GetAvailableAdapters()
        GetRequiredAdapter(bankCode)
    }

    class BankGatewayService {
        SubmitPaymentAsync(payment)
        CheckPaymentStatusAsync(payment)
    }

    class MockBankAdapter
    class TBankSandboxAdapter

    IBankAdapter <|.. MockBankAdapter
    IBankAdapter <|.. TBankSandboxAdapter
    BankGatewayService --> IBankAdapterRegistry
    IBankAdapterRegistry --> IBankAdapter
```

Новые банки подключаются добавлением новых классов-адаптеров, реализующих `IBankAdapter`, без изменения основной бизнес-логики платежей.

## Последовательность платежа

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    actor Manager as Руководитель
    participant Web as Razor Pages
    participant Payment as PaymentOrderService
    participant Approval as ApprovalService
    participant Bank as BankProcessingService
    participant Adapter as IBankAdapter
    participant Audit as AuditLogService

    Accountant->>Web: Создает платеж
    Web->>Payment: CreateAsync(dto)
    Payment->>Payment: Проверка реквизитов, дублей, валюты, маршрута
    Payment->>Audit: PaymentCreated
    Accountant->>Web: Отправляет на согласование
    Web->>Approval: SubmitForApprovalAsync(paymentId)
    Approval->>Audit: PaymentSubmittedForApproval
    Manager->>Web: Утверждает платеж
    Web->>Approval: ApproveAsync(paymentId)
    Approval->>Audit: PaymentApproved
    Approval->>Bank: SendToBankAsync(paymentId)
    Bank->>Bank: SignedAt + SignatureReference
    Bank->>Audit: PaymentSigned
    Bank->>Adapter: SubmitPaymentAsync(payment, connection)
    Adapter-->>Bank: ReferenceId + accepted
    Bank->>Audit: PaymentSentToBank
```

## Последовательность выписки и сверки

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant Web as Statements Page
    participant Statement as BankStatementService
    participant Registry as IBankAdapterRegistry
    participant Adapter as IBankAdapter
    participant Db as SQLite
    participant Audit as AuditLogService

    Accountant->>Web: Выбирает счет и период
    Web->>Statement: LoadDemoStatementAsync(accountId, period)
    Statement->>Registry: GetRequiredAdapter(bankCode)
    Registry-->>Statement: Adapter
    Statement->>Adapter: LoadStatementAsync(account, connection, period)
    Adapter-->>Statement: Operations
    Statement->>Db: Save RawDataJson
    Statement->>Statement: Match by BankReferenceId, amount, date, account, purpose
    Statement->>Db: Update matched/unmatched
    Statement->>Audit: BankStatementImported + Reconciled
```

## Безопасность

- Авторизация построена на ASP.NET Identity и ролях `Accountant`, `Manager`, `Administrator`, `PlatformAdmin`.
- Данные пользователей организации фильтруются через `CurrentOrganizationService`.
- Банковские токены сохраняются в `ProtectedAccessToken` и `ProtectedRefreshToken`.
- Data Protection настроен на локальное хранение ключей web-проекта, чтобы прототип работал без доступа к пользовательскому `AppData`.
- Для ВКР используется имитация подписи: `SignedAt` и `SignatureReference`, без реальной криптографии.

## Ограничения прототипа

- Реальная криптоподпись не реализована.
- Реальные банковские сертификаты и mTLS не настраиваются.
- Sequential/parallel согласование хранится как тип маршрута, но фактически решение принимает любой руководитель организации.
- T-Банк Sandbox зависит от TLS/сертификатной конфигурации локальной Windows-среды.
- В части старых Razor/CS файлов еще могут встречаться mojibake-строки. Перед финальной демонстрацией нужно пройтись по UI и исправить нечитаемый текст.
