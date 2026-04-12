using Microsoft.EntityFrameworkCore;
using OpenPay.Application.DTOs.Approvals;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class ApprovalService : IApprovalService
{
    private readonly OpenPayDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;


    public ApprovalService(OpenPayDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task SubmitForApprovalAsync(Guid paymentOrderId)
    {
        var payment = await _dbContext.PaymentOrders.FirstOrDefaultAsync(x => x.Id == paymentOrderId);

        if (payment == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (payment.Status != PaymentStatus.Draft && payment.Status != PaymentStatus.Rework)
            throw new InvalidOperationException("На согласование можно отправить только платеж в статусе Draft или Rework.");

        payment.Status = PaymentStatus.PendingApproval;
        await _dbContext.SaveChangesAsync();
        await _auditLogService.LogAsync(
            AuditEventType.PaymentSubmittedForApproval,
            payment.CreatedByUserId,
            $"Платеж {payment.DocumentNumber} отправлен на согласование",
            payment.Id.ToString(),
            nameof(PaymentOrder));
    }

    public async Task<IReadOnlyList<PendingApprovalListItemDto>> GetPendingApprovalsAsync()
    {
        return await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Counterparty)
            .Where(x => x.Status == PaymentStatus.PendingApproval)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PendingApprovalListItemDto
            {
                PaymentOrderId = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedAt = x.CreatedAt,
                PaymentDate = x.PaymentDate,
                CounterpartyName = x.Counterparty!.FullName,
                Amount = x.Amount,
                Currency = x.Currency,
                Purpose = x.Purpose
            })
            .ToListAsync();
    }

    public async Task<ApprovalReviewDto?> GetReviewModelAsync(Guid paymentOrderId)
    {
        return await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Counterparty)
            .Include(x => x.OrganizationBankAccount)
            .Where(x => x.Id == paymentOrderId)
            .Select(x => new ApprovalReviewDto
            {
                PaymentOrderId = x.Id,
                DocumentNumber = x.DocumentNumber,
                CreatedAt = x.CreatedAt,
                PaymentDate = x.PaymentDate,
                CounterpartyName = x.Counterparty!.FullName,
                CounterpartyInn = x.Counterparty.Inn,
                OrganizationAccountDisplay = x.OrganizationBankAccount!.BankName + " / " + x.OrganizationBankAccount.AccountNumber,
                Amount = x.Amount,
                Currency = x.Currency,
                Purpose = x.Purpose,
                Status = x.Status.ToString()
            })
            .FirstOrDefaultAsync();
    }

    public async Task ApproveAsync(Guid paymentOrderId, string approverUserId, string? comment)
    {
        var payment = await GetPendingPaymentAsync(paymentOrderId);

        payment.Status = PaymentStatus.Approved;

        _dbContext.ApprovalDecisions.Add(new ApprovalDecision
        {
            PaymentOrderId = payment.Id,
            ApproverUserId = approverUserId,
            Decision = ApprovalDecisionType.Approved,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        });

        await _dbContext.SaveChangesAsync();
        await _auditLogService.LogAsync(
            AuditEventType.PaymentApproved,
            approverUserId,
            $"Платеж {payment.DocumentNumber} утвержден",
            payment.Id.ToString(),
            nameof(PaymentOrder));
    }

    public async Task RejectAsync(Guid paymentOrderId, string approverUserId, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("При отклонении необходимо указать комментарий.");

        var payment = await GetPendingPaymentAsync(paymentOrderId);

        payment.Status = PaymentStatus.Rejected;

        _dbContext.ApprovalDecisions.Add(new ApprovalDecision
        {
            PaymentOrderId = payment.Id,
            ApproverUserId = approverUserId,
            Decision = ApprovalDecisionType.Rejected,
            Comment = comment.Trim()
        });

        await _dbContext.SaveChangesAsync();
        await _auditLogService.LogAsync(
            AuditEventType.PaymentRejected,
            approverUserId,
            $"Платеж {payment.DocumentNumber} отклонен. Комментарий: {comment.Trim()}",
            payment.Id.ToString(),
            nameof(PaymentOrder));
    }

    public async Task ReturnForReworkAsync(Guid paymentOrderId, string approverUserId, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new InvalidOperationException("При возврате на доработку необходимо указать комментарий.");

        var payment = await GetPendingPaymentAsync(paymentOrderId);

        payment.Status = PaymentStatus.Rework;

        _dbContext.ApprovalDecisions.Add(new ApprovalDecision
        {
            PaymentOrderId = payment.Id,
            ApproverUserId = approverUserId,
            Decision = ApprovalDecisionType.Rework,
            Comment = comment.Trim()
        });

        await _dbContext.SaveChangesAsync();
        await _auditLogService.LogAsync(
            AuditEventType.PaymentReturnedForRework,
            approverUserId,
            $"Платеж {payment.DocumentNumber} возвращен на доработку. Комментарий: {comment.Trim()}",
            payment.Id.ToString(),
            nameof(PaymentOrder));
    }

    private async Task<PaymentOrder> GetPendingPaymentAsync(Guid paymentOrderId)
    {
        var payment = await _dbContext.PaymentOrders.FirstOrDefaultAsync(x => x.Id == paymentOrderId);

        if (payment == null)
            throw new InvalidOperationException("Платежное поручение не найдено.");

        if (payment.Status != PaymentStatus.PendingApproval)
            throw new InvalidOperationException("Рассмотреть можно только платеж в статусе PendingApproval.");

        return payment;
    }
    public async Task<IReadOnlyList<ApprovalDecisionHistoryItemDto>> GetHistoryAsync(Guid paymentOrderId)
    {
        var history = await
            (from decision in _dbContext.ApprovalDecisions.AsNoTracking()
             where decision.PaymentOrderId == paymentOrderId
             join user in _dbContext.Users.AsNoTracking()
                 on decision.ApproverUserId equals user.Id into users
             from user in users.DefaultIfEmpty()
             orderby decision.CreatedAt descending
             select new ApprovalDecisionHistoryItemDto
             {
                 CreatedAt = decision.CreatedAt,
                 ApproverName = user != null ? user.FullName : decision.ApproverUserId,
                 Decision = decision.Decision.ToString(),
                 Comment = decision.Comment
             })
            .ToListAsync();

        return history;
    }
}