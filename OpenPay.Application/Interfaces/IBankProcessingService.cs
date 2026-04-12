namespace OpenPay.Application.Interfaces;

public interface IBankProcessingService
{
    Task SendToBankAsync(Guid paymentOrderId, string userId);
    Task CheckBankStatusAsync(Guid paymentOrderId, string userId);
}