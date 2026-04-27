# OpenPay

OpenPay is a Razor Pages prototype of a corporate payment aggregator for a graduation project. It demonstrates counterparties, organization accounts, payment orders, approval, demo OpenBanking adapters, bank statements, reconciliation, reports and audit.

## Run

```powershell
dotnet restore
dotnet run --project OpenPay.Web
```

The app uses SQLite by default: `OpenPay.Web/openpay-dev.db`. On startup it applies EF migrations and seeds demo data.

## Demo Users

| Role | Login | Password |
| --- | --- | --- |
| Platform admin | `platformadmin@openpay.local` | `Admin123!` |
| Organization admin | `admin@openpay.local` | `Admin123!` |
| Accountant | `accountant@openpay.local` | `Accountant123!` |
| Manager | `manager@openpay.local` | `Manager123!` |

## CSV Formats

Counterparties:

```csv
INN;Name;KPP;BIC;AccountNumber;CorrespondentAccount
7707083893;ООО "Ромашка";770701001;044525225;40702810000000000007;30101810400000000225
500100732259;ООО "Вектор";500101001;044030653;40702810000000000002;30101810500000000653
```

Payments:

```csv
DocumentNumber;PaymentDate;CounterpartyInn;OrganizationAccountNumber;Amount;Currency;ExpenseType;Purpose
PAY-001;15.05.2026;7707083893;40702810000000000007;12500,50;RUB;Поставщики;Оплата поставщику по договору №15
```

`DocumentNumber` and `ExpenseType` are optional. The prototype validates INN, KPP, BIC, account control keys, duplicate payments, account currency and active organization scope.

## Demo Flow

1. Sign in as accountant and create or import a payment.
2. Send the draft to approval.
3. Sign in as manager and approve it.
4. The system simulates signing and sends the payment through a demo bank adapter.
5. Open `Выписки`, load a demo statement and run reconciliation.
6. Use `Отчетность` and `Аудит` to inspect totals and history.
