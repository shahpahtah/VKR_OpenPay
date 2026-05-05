# Диаграммы последовательностей OpenPay

Документ содержит диаграммы последовательностей для ключевых бизнес-сценариев OpenPay. Участники диаграмм указаны на уровне модулей проекта: Razor Pages UI, сервисы, EF Core/SQLite, ASP.NET Identity, банковский шлюз, адаптеры и аудит.

## 1. Вход пользователя и определение организации

```mermaid
sequenceDiagram
    actor User as Пользователь
    participant LoginPage as "OpenPay.Web<br/>Login.cshtml"
    participant Identity as "ASP.NET Identity<br/>SignInManager/UserManager"
    participant Db as "OpenPay.Infrastructure<br/>OpenPayDbContext"
    participant Middleware as "OrganizationStateMiddleware"
    participant CurrentOrg as "CurrentOrganizationService"
    participant IndexPage as "IndexModel"

    User->>LoginPage: Вводит email и пароль
    LoginPage->>Identity: PasswordSignInAsync(email, password)
    Identity->>Db: Поиск пользователя, ролей и password hash
    Db-->>Identity: ApplicationUser + Identity roles
    Identity-->>LoginPage: Успешный вход
    LoginPage-->>User: Redirect на главную страницу

    User->>IndexPage: GET /
    IndexPage->>Middleware: Запрос проходит через middleware
    Middleware->>CurrentOrg: GetCurrentOrganizationInfoAsync()
    CurrentOrg->>Identity: Получить текущего пользователя
    CurrentOrg->>Db: Найти организацию пользователя
    Db-->>CurrentOrg: Organization
    CurrentOrg-->>Middleware: CurrentOrganizationDto
    Middleware-->>IndexPage: Организация активна
    IndexPage-->>User: Рабочая панель по роли пользователя
```

## 2. Создание платежа бухгалтером

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant Page as "OpenPay.Web<br/>Payments/Create"
    participant PaymentService as "PaymentOrderService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Validators as "BankingValidators"
    participant Db as "OpenPayDbContext"
    participant RouteService as "ApprovalRoute selection<br/>(внутри PaymentOrderService)"
    participant Audit as "AuditLogService"

    Accountant->>Page: Заполняет форму платежа
    Page->>PaymentService: CreateAsync(dto, createdByUserId)
    PaymentService->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>PaymentService: organizationId

    PaymentService->>Db: Загрузить контрагента и счет организации
    Db-->>PaymentService: Counterparty + OrganizationBankAccount

    PaymentService->>Validators: Проверить ИНН/КПП/БИК/счета
    Validators-->>PaymentService: Реквизиты корректны или ошибка

    PaymentService->>PaymentService: Проверить валюту платежа и счета
    PaymentService->>PaymentService: Проверить возможный дубль платежа
    PaymentService->>Db: Найти активный маршрут по сумме, типу расхода и подразделению
    Db-->>RouteService: ApprovalRoute
    RouteService-->>PaymentService: approvalRouteId

    PaymentService->>Db: Создать PaymentOrder со статусом Draft
    Db-->>PaymentService: paymentOrderId
    PaymentService->>Audit: LogAsync(PaymentCreated)
    Audit->>Db: Сохранить AuditLogEntry
    PaymentService-->>Page: Id созданного платежа
    Page-->>Accountant: Redirect на список платежей
```

## 3. Отправка платежа на согласование

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant PaymentsPage as "OpenPay.Web<br/>Payments/Index"
    participant ApprovalService as "ApprovalService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Db as "OpenPayDbContext"
    participant Audit as "AuditLogService"

    Accountant->>PaymentsPage: Нажимает "На согласование"
    PaymentsPage->>ApprovalService: SubmitForApprovalAsync(paymentOrderId)
    ApprovalService->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>ApprovalService: organizationId
    ApprovalService->>Db: Найти PaymentOrder текущей организации
    Db-->>ApprovalService: PaymentOrder
    ApprovalService->>ApprovalService: Проверить допустимый статус
    ApprovalService->>Db: Status = PendingApproval
    ApprovalService->>Audit: LogAsync(PaymentSubmittedForApproval)
    Audit->>Db: Сохранить AuditLogEntry
    ApprovalService-->>PaymentsPage: OK
    PaymentsPage-->>Accountant: Платеж отображается как "На согласовании"
```

## 4. Рассмотрение платежа руководителем

```mermaid
sequenceDiagram
    actor Manager as Руководитель
    participant ApprovalsPage as "OpenPay.Web<br/>Approvals/Index"
    participant ReviewPage as "OpenPay.Web<br/>Approvals/Review"
    participant ApprovalService as "ApprovalService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Db as "OpenPayDbContext"

    Manager->>ApprovalsPage: Открывает очередь согласования
    ApprovalsPage->>ApprovalService: GetPendingApprovalsAsync()
    ApprovalService->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>ApprovalService: organizationId
    ApprovalService->>Db: Найти платежи PendingApproval в организации
    Db-->>ApprovalService: PendingApprovalListItemDto[]
    ApprovalService-->>ApprovalsPage: Очередь платежей
    ApprovalsPage-->>Manager: Список документов

    Manager->>ReviewPage: Открывает карточку платежа
    ReviewPage->>ApprovalService: GetReviewModelAsync(paymentOrderId)
    ApprovalService->>Db: Загрузить платеж, контрагента, счет, маршрут
    Db-->>ApprovalService: ApprovalReviewDto
    ApprovalService-->>ReviewPage: Модель рассмотрения
    ReviewPage-->>Manager: Карточка платежа и история решений
```

## 5. Утверждение платежа и автоматическая отправка в банк

```mermaid
sequenceDiagram
    actor Manager as Руководитель
    participant ReviewPage as "OpenPay.Web<br/>Approvals/Review"
    participant ApprovalService as "ApprovalService"
    participant Db as "OpenPayDbContext"
    participant Audit as "AuditLogService"
    participant BankProcessing as "BankProcessingService"
    participant Gateway as "BankGatewayService"
    participant Registry as "BankAdapterRegistry"
    participant Adapter as "IBankAdapter<br/>MOCK или TBANK_SANDBOX"

    Manager->>ReviewPage: Нажимает "Утвердить"
    ReviewPage->>ApprovalService: ApproveAsync(paymentOrderId, approverUserId, comment)

    ApprovalService->>Db: Загрузить PaymentOrder
    Db-->>ApprovalService: PaymentOrder
    ApprovalService->>Db: Создать ApprovalDecision(Approved)
    ApprovalService->>Db: Status = Approved / ReadyToSend
    ApprovalService->>Audit: LogAsync(PaymentApproved)
    Audit->>Db: Сохранить AuditLogEntry

    ApprovalService->>BankProcessing: SendToBankAsync(paymentOrderId, approverUserId)
    BankProcessing->>Db: Загрузить платеж, счет, подключение, контрагента
    Db-->>BankProcessing: PaymentOrder aggregate
    BankProcessing->>BankProcessing: Создать demo-подпись SignedAt + SignatureReference
    BankProcessing->>Audit: LogAsync(PaymentSigned)
    Audit->>Db: Сохранить AuditLogEntry

    BankProcessing->>Gateway: SubmitPaymentAsync(payment)
    Gateway->>Gateway: Получить BankConnection из OrganizationBankAccount
    Gateway->>Registry: GetRequiredAdapter(bankCode)
    Registry-->>Gateway: IBankAdapter
    Gateway->>Adapter: SubmitPaymentAsync(payment, connection)
    Adapter-->>Gateway: BankSubmitResultDto
    Gateway-->>BankProcessing: Результат отправки

    alt Банк принял платеж
        BankProcessing->>Db: Status = Sent, BankReferenceId, SentAt, BankResponseMessage
        BankProcessing->>Audit: LogAsync(PaymentSentToBank)
    else Банк вернул ошибку
        BankProcessing->>Db: Status = Error, BankResponseMessage, ProcessedAt
        BankProcessing->>Audit: LogAsync(PaymentBankError)
    end

    BankProcessing-->>ApprovalService: OK
    ApprovalService-->>ReviewPage: OK
    ReviewPage-->>Manager: Платеж утвержден и отправлен
```

## 6. Отклонение платежа или возврат на доработку

```mermaid
sequenceDiagram
    actor Manager as Руководитель
    participant ReviewPage as "OpenPay.Web<br/>Approvals/Review"
    participant ApprovalService as "ApprovalService"
    participant Db as "OpenPayDbContext"
    participant Audit as "AuditLogService"
    participant Accountant as Бухгалтер

    alt Отклонение
        Manager->>ReviewPage: Нажимает "Отклонить" и вводит комментарий
        ReviewPage->>ApprovalService: RejectAsync(paymentOrderId, approverUserId, comment)
        ApprovalService->>Db: Создать ApprovalDecision(Rejected)
        ApprovalService->>Db: Status = Rejected
        ApprovalService->>Audit: LogAsync(PaymentRejected)
        Audit->>Db: Сохранить AuditLogEntry
        ReviewPage-->>Manager: Платеж отклонен
    else Возврат на доработку
        Manager->>ReviewPage: Нажимает "На доработку" и вводит комментарий
        ReviewPage->>ApprovalService: ReturnForReworkAsync(paymentOrderId, approverUserId, comment)
        ApprovalService->>Db: Создать ApprovalDecision(Rework)
        ApprovalService->>Db: Status = Rework
        ApprovalService->>Audit: LogAsync(PaymentReturnedForRework)
        Audit->>Db: Сохранить AuditLogEntry
        ReviewPage-->>Manager: Платеж возвращен
        Accountant->>ReviewPage: Видит статус "На доработке" в списке платежей
    end
```

## 7. Ручная fallback-отправка платежа в банк

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant PaymentsPage as "OpenPay.Web<br/>Payments/Index"
    participant BankProcessing as "BankProcessingService"
    participant Db as "OpenPayDbContext"
    participant Gateway as "BankGatewayService"
    participant Registry as "BankAdapterRegistry"
    participant Adapter as "IBankAdapter"
    participant Audit as "AuditLogService"

    Accountant->>PaymentsPage: Нажимает "В банк" у Approved/ReadyToSend платежа
    PaymentsPage->>BankProcessing: SendToBankAsync(paymentOrderId, userId)
    BankProcessing->>Db: Загрузить платеж со счетом и подключением
    Db-->>BankProcessing: PaymentOrder aggregate
    BankProcessing->>BankProcessing: Если подписи нет, создать demo-подпись
    BankProcessing->>Gateway: SubmitPaymentAsync(payment)
    Gateway->>Registry: GetRequiredAdapter(bankCode)
    Registry-->>Gateway: Adapter
    Gateway->>Adapter: SubmitPaymentAsync(payment, connection)
    Adapter-->>Gateway: BankSubmitResultDto
    Gateway-->>BankProcessing: Результат
    BankProcessing->>Db: Обновить статус, BankReferenceId и сообщение банка
    BankProcessing->>Audit: LogAsync(PaymentSentToBank или PaymentBankError)
    Audit->>Db: Сохранить AuditLogEntry
    PaymentsPage-->>Accountant: Обновленный статус платежа
```

## 8. Фоновая проверка банковских статусов

```mermaid
sequenceDiagram
    participant Hosted as "BankStatusBackgroundService"
    participant Processor as "BankStatusProcessor"
    participant Db as "OpenPayDbContext"
    participant Gateway as "BankGatewayService"
    participant Registry as "BankAdapterRegistry"
    participant Adapter as "IBankAdapter"
    participant Audit as "AuditLogService"

    loop Периодически
        Hosted->>Processor: ProcessPendingStatusesAsync(cancellationToken)
        Processor->>Db: Найти платежи Sent с BankReferenceId
        Db-->>Processor: PaymentOrder[]

        loop По каждому платежу
            Processor->>Gateway: CheckPaymentStatusAsync(payment)
            Gateway->>Registry: GetRequiredAdapter(bankCode)
            Registry-->>Gateway: Adapter
            Gateway->>Adapter: CheckPaymentStatusAsync(payment, connection)
            Adapter-->>Gateway: BankStatusResultDto
            Gateway-->>Processor: Статус банка

            alt Финальный статус получен
                Processor->>Db: Status = Executed или Error, ProcessedAt, BankResponseMessage
                Processor->>Audit: LogAsync(PaymentExecutedByBank или PaymentBankError)
            else Статус еще промежуточный
                Processor->>Db: Сохранить сообщение без финального завершения
            end
        end
    end
```

## 9. Создание банковского подключения

```mermaid
sequenceDiagram
    actor Admin as Администратор
    participant Page as "OpenPay.Web<br/>Admin/BankConnections/Create"
    participant Service as "BankConnectionService"
    participant Registry as "BankAdapterRegistry"
    participant Protection as "TokenProtectionService<br/>ASP.NET Data Protection"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Db as "OpenPayDbContext"
    participant Audit as "AuditLogService"

    Admin->>Page: Заполняет банк, название, access token
    Page->>Service: CreateAsync(dto, userId)
    Service->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>Service: organizationId
    Service->>Registry: GetRequiredAdapter(dto.BankCode)
    Registry-->>Service: IBankAdapter
    Service->>Service: Проверить уникальность названия
    Service->>Protection: Protect(accessToken / refreshToken)
    Protection-->>Service: ProtectedAccessToken / ProtectedRefreshToken
    Service->>Db: Создать BankConnection
    Db-->>Service: bankConnectionId
    Service->>Audit: LogAsync(BankConnectionCreated)
    Audit->>Db: Сохранить AuditLogEntry
    Service-->>Page: Id подключения
    Page-->>Admin: Redirect на список подключений
```

## 10. Загрузка выписки и сверка

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant Page as "OpenPay.Web<br/>Statements/Index"
    participant StatementService as "BankStatementService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Db as "OpenPayDbContext"
    participant Registry as "BankAdapterRegistry"
    participant Adapter as "IBankAdapter"
    participant Audit as "AuditLogService"

    Accountant->>Page: Выбирает счет и период, нажимает "Загрузить"
    Page->>StatementService: LoadDemoStatementAsync(accountId, periodFrom, periodTo, userId)
    StatementService->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>StatementService: organizationId
    StatementService->>Db: Загрузить OrganizationBankAccount + BankConnection
    Db-->>StatementService: Счет и подключение
    StatementService->>Registry: GetRequiredAdapter(bankCode)
    Registry-->>StatementService: Adapter
    StatementService->>Adapter: LoadStatementAsync(account, connection, period)
    Adapter-->>StatementService: BankStatementOperationDto[]
    StatementService->>Db: Сохранить BankStatement.RawDataJson
    StatementService->>StatementService: Reconcile operations with PaymentOrders
    StatementService->>Db: Обновить Total/Matched/Unmatched
    StatementService->>Audit: LogAsync(BankStatementImported)
    StatementService->>Audit: LogAsync(BankStatementReconciled)
    Audit->>Db: Сохранить AuditLogEntry
    StatementService-->>Page: BankStatementResultDto
    Page-->>Accountant: Результат сверки
```

## 11. CSV-импорт контрагентов

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant Page as "OpenPay.Web<br/>Counterparties/Import"
    participant Service as "CounterpartyService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Validators as "BankingValidators"
    participant Db as "OpenPayDbContext"

    Accountant->>Page: Загружает CSV-файл
    Page->>Service: ImportFromCsvAsync(csvStream)
    Service->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>Service: organizationId

    loop Каждая строка CSV
        Service->>Service: Распознать колонки и значения
        Service->>Validators: Проверить ИНН/КПП/БИК/счета
        Validators-->>Service: OK или текст ошибки
        alt Строка корректна
            Service->>Db: Создать или обновить Counterparty
        else Строка некорректна
            Service->>Service: Добавить ошибку в CounterpartyImportRowResultDto
        end
    end

    Service-->>Page: CounterpartyImportResultDto
    Page-->>Accountant: Количество успешных строк и ошибки
```

## 12. CSV-импорт платежей

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant Page as "OpenPay.Web<br/>Payments/Import"
    participant Service as "PaymentOrderService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Db as "OpenPayDbContext"
    participant Validators as "BankingValidators"

    Accountant->>Page: Загружает CSV платежей
    Page->>Service: ImportFromCsvAsync(csvStream, createdByUserId)
    Service->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>Service: organizationId

    loop Каждая строка CSV
        Service->>Service: Распознать document/date/inn/account/amount/currency
        Service->>Db: Найти Counterparty по ИНН
        Service->>Db: Найти OrganizationBankAccount по номеру счета
        Service->>Validators: Проверить реквизиты и обязательные поля
        Service->>Service: Проверить валюту счета и дубли
        Service->>Db: Найти ApprovalRoute

        alt Строка корректна
            Service->>Db: Создать PaymentOrder в Draft
        else Ошибка данных
            Service->>Service: Добавить ошибку в PaymentOrderImportRowResultDto
        end
    end

    Service-->>Page: PaymentOrderImportResultDto
    Page-->>Accountant: Результаты импорта
```

## 13. Формирование отчета и экспорт

```mermaid
sequenceDiagram
    actor Accountant as Бухгалтер
    participant Page as "OpenPay.Web<br/>Reports/Index"
    participant ReportService as "ReportService"
    participant ExportService as "ReportExportService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Db as "OpenPayDbContext"

    Accountant->>Page: Открывает отчеты и выбирает период
    Page->>ReportService: GetOverviewAsync(dateFrom, dateTo)
    ReportService->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>ReportService: organizationId
    ReportService->>Db: Загрузить платежи организации за период
    Db-->>ReportService: PaymentOrder[]
    ReportService->>ReportService: Сгруппировать по статусам
    ReportService->>ReportService: Сгруппировать по контрагентам
    ReportService-->>Page: ReportOverviewDto
    Page-->>Accountant: Отчет на странице

    alt Экспорт CSV
        Accountant->>Page: Нажимает "CSV"
        Page->>ExportService: ExportToCsv(report)
        ExportService-->>Page: byte[]
        Page-->>Accountant: Файл CSV
    else Экспорт Excel
        Accountant->>Page: Нажимает "Excel"
        Page->>ExportService: ExportToExcel(report)
        ExportService-->>Page: byte[]
        Page-->>Accountant: Файл XLSX
    end
```

## 14. Управление пользователями и синхронизация ролей

```mermaid
sequenceDiagram
    actor Admin as Администратор
    participant Page as "OpenPay.Web<br/>Admin/Users/Create/Edit"
    participant Service as "UserManagementService"
    participant CurrentOrg as "CurrentOrganizationService"
    participant Identity as "ASP.NET Identity<br/>UserManager"
    participant Db as "OpenPayDbContext"

    Admin->>Page: Создает или редактирует сотрудника
    Page->>Service: CreateUserAsync(dto) или UpdateAsync(dto)
    Service->>CurrentOrg: GetRequiredOrganizationIdAsync()
    CurrentOrg-->>Service: organizationId

    alt Создание
        Service->>Identity: CreateAsync(ApplicationUser, password)
        Identity->>Db: Создать пользователя Identity
    else Редактирование
        Service->>Identity: FindByIdAsync(userId)
        Identity->>Db: Найти пользователя
        Service->>Identity: UpdateAsync(user)
    end

    Service->>Identity: GetRolesAsync(user)
    Identity-->>Service: Текущие Identity-роли
    Service->>Identity: RemoveFromRolesAsync(старые прикладные роли)
    Service->>Identity: AddToRoleAsync(user, dto.Role)
    Identity->>Db: Обновить AspNetUserRoles
    Service-->>Page: OK
    Page-->>Admin: Пользователь сохранен
```

## 15. Выбор маршрута согласования при создании платежа

```mermaid
sequenceDiagram
    participant PaymentService as "PaymentOrderService"
    participant Db as "OpenPayDbContext"
    participant Account as "OrganizationBankAccount"
    participant Route as "ApprovalRoute"

    PaymentService->>Db: Загрузить счет организации
    Db-->>PaymentService: OrganizationBankAccount
    PaymentService->>Account: Взять ResponsibleUnit и Currency
    Account-->>PaymentService: Подразделение и валюта

    PaymentService->>Db: Найти активные ApprovalRoute организации
    Db-->>PaymentService: ApprovalRoute[]

    loop Каждый маршрут
        PaymentService->>Route: Проверить MinAmount/MaxAmount
        PaymentService->>Route: Проверить ExpenseType
        PaymentService->>Route: Проверить Department = ResponsibleUnit
    end

    alt Найден подходящий маршрут
        PaymentService->>PaymentService: payment.ApprovalRouteId = route.Id
    else Маршрут не найден
        PaymentService->>PaymentService: Ошибка валидации создания платежа
    end
```

## Итоговая связка модулей

```mermaid
sequenceDiagram
    actor User as Пользователь
    participant Web as "OpenPay.Web<br/>Razor Pages"
    participant App as "OpenPay.Application<br/>Interfaces + DTO"
    participant Infra as "OpenPay.Infrastructure<br/>Services"
    participant Domain as "OpenPay.Domain<br/>Entities + Enums"
    participant Db as "SQLite / EF Core"
    participant Bank as "External/Mock Bank<br/>IBankAdapter"

    User->>Web: Действие в интерфейсе
    Web->>App: Вызов интерфейса сервиса
    App->>Infra: Реализация сервиса
    Infra->>Domain: Работа с сущностями и enum
    Infra->>Db: Чтение/запись через OpenPayDbContext
    Infra->>Bank: При необходимости вызов банковского адаптера
    Bank-->>Infra: Результат операции
    Infra-->>Web: DTO / результат / ошибка
    Web-->>User: Обновленная страница
```
