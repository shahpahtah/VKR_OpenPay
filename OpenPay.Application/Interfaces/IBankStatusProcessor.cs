namespace OpenPay.Application.Interfaces;

public interface IBankStatusProcessor
{
    Task ProcessPendingStatusesAsync(CancellationToken cancellationToken);
}