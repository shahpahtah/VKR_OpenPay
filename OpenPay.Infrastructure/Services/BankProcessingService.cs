using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class BankProcessingService : IBankProcessingService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly IBankGatewayService _bankGatewayService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentOrganizationService _currentOrganizationService;

    public BankProcessingService(
        OpenPayDbContext dbContext,
        IBankGatewayService bankGatewayService,
        IAuditLogService auditLogService,
        ICurrentOrganizationService currentOrganizationService)
    {
        _dbContext = dbContext;
        _bankGatewayService = bankGatewayService;
        _auditLogService = auditLogService;
        _currentOrganizationService = currentOrganizationService;
    }

    public async Task SendToBankAsync(Guid paymentOrderId, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var payment = await _dbContext.PaymentOrders
            .Include(x => x.OrganizationBankAccount)
            .ThenInclude(x => x!.BankConnection)
            .FirstOrDefaultAsync(x => x.Id == paymentOrderId && x.OrganizationId == organizationId);

        if (payment == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (payment.Status != PaymentStatus.Approved && payment.Status != PaymentStatus.ReadyToSend)
            throw new InvalidOperationException("В банк можно отправить только платеж в статусе Approved или ReadyToSend.");

        if (payment.SignedAt == null)
        {
            payment.SignedAt = DateTime.UtcNow;
            payment.SignatureReference = $"SIGN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];
            payment.Status = PaymentStatus.ReadyToSend;

            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                AuditEventType.PaymentSigned,
                userId,
                $"Платеж {payment.DocumentNumber} подписан демо-подписью {payment.SignatureReference}",
                payment.Id.ToString(),
                nameof(PaymentOrder));
        }

        var result = await _bankGatewayService.SubmitPaymentAsync(payment);

        payment.BankReferenceId = result.ReferenceId;
        payment.BankResponseMessage = result.Message;

        if (!result.IsAccepted)
        {
            payment.Status = PaymentStatus.Error;
            payment.ProcessedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                AuditEventType.PaymentBankError,
                userId,
                $"Банк не принял платеж {payment.DocumentNumber}: {result.Message}",
                payment.Id.ToString(),
                nameof(PaymentOrder));

            return;
        }

        payment.Status = PaymentStatus.Sent;
        payment.SentAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            AuditEventType.PaymentSentToBank,
            userId,
            $"Платеж {payment.DocumentNumber} отправлен в банк. Reference: {result.ReferenceId}",
            payment.Id.ToString(),
            nameof(PaymentOrder));
    }

    public async Task CheckBankStatusAsync(Guid paymentOrderId, string userId)
    {
        var organizationId = await _currentOrganizationService.GetRequiredOrganizationIdAsync();

        var payment = await _dbContext.PaymentOrders
            .Include(x => x.OrganizationBankAccount)
            .ThenInclude(x => x!.BankConnection)
            .FirstOrDefaultAsync(x => x.Id == paymentOrderId && x.OrganizationId == organizationId);

        if (payment == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (payment.Status != PaymentStatus.Sent)
            throw new InvalidOperationException("Проверить статус можно только для платежа в статусе Sent.");

        var result = await _bankGatewayService.CheckPaymentStatusAsync(payment);

        payment.Status = result.FinalStatus;
        payment.BankResponseMessage = result.Message;
        payment.ProcessedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var auditEvent = result.FinalStatus == PaymentStatus.Executed
            ? AuditEventType.PaymentExecutedByBank
            : AuditEventType.PaymentBankError;

        var description = result.FinalStatus == PaymentStatus.Executed
            ? $"Платеж {payment.DocumentNumber} исполнен банком."
            : $"Платеж {payment.DocumentNumber} завершился ошибкой при обработке банком.";

        await _auditLogService.LogAsync(
            auditEvent,
            userId,
            description,
            payment.Id.ToString(),
            nameof(PaymentOrder));
    }
}
