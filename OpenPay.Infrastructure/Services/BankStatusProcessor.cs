using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class BankStatusProcessor : IBankStatusProcessor
{
    private readonly OpenPayDbContext _dbContext;
    private readonly IBankGatewayService _bankGatewayService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BankStatusProcessor> _logger;

    public BankStatusProcessor(
        OpenPayDbContext dbContext,
        IBankGatewayService bankGatewayService,
        IAuditLogService auditLogService,
        ILogger<BankStatusProcessor> logger)
    {
        _dbContext = dbContext;
        _bankGatewayService = bankGatewayService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task ProcessPendingStatusesAsync(CancellationToken cancellationToken)
    {
        var payments = await _dbContext.PaymentOrders
            .Include(x => x.OrganizationBankAccount)
            .ThenInclude(x => x!.BankConnection)
            .Where(x => x.Status == PaymentStatus.Sent)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);

        if (payments.Count == 0)
            return;

        _logger.LogInformation("Найдено {Count} платежей в статусе Sent для фоновой обработки.", payments.Count);

        foreach (var payment in payments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _bankGatewayService.CheckPaymentStatusAsync(payment);

                payment.Status = result.FinalStatus;
                payment.BankResponseMessage = result.Message;
                payment.ProcessedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                var auditEvent = result.FinalStatus == PaymentStatus.Executed
                    ? AuditEventType.PaymentExecutedByBank
                    : AuditEventType.PaymentBankError;

                var description = result.FinalStatus == PaymentStatus.Executed
                    ? $"Платеж {payment.DocumentNumber} исполнен банком в фоне."
                    : $"Платеж {payment.DocumentNumber} завершился ошибкой при фоновой обработке банком.";

                await _auditLogService.LogAsync(
                    auditEvent,
                    payment.CreatedByUserId,
                    description,
                    payment.Id.ToString(),
                    nameof(OpenPay.Domain.Entities.PaymentOrder));

                _logger.LogInformation(
                    "Платеж {DocumentNumber} обработан фоном. Новый статус: {Status}",
                    payment.DocumentNumber,
                    payment.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ошибка при фоновой обработке платежа {DocumentNumber}",
                    payment.DocumentNumber);
            }
        }
    }
}
