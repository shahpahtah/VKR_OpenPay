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
            .FirstOrDefaultAsync(x => x.Id == paymentOrderId && x.OrganizationId == organizationId);

        if (payment == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (payment.Status != PaymentStatus.Approved)
            throw new InvalidOperationException("В банк можно отправить только платеж в статусе Approved.");

        var result = await _bankGatewayService.SubmitPaymentAsync(payment);

        if (!result.IsAccepted)
            throw new InvalidOperationException("Банк не принял платеж к обработке.");

        payment.Status = PaymentStatus.Sent;
        payment.BankReferenceId = result.ReferenceId;
        payment.BankResponseMessage = result.Message;
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