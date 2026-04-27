using OpenPay.Application.DTOs.Banking;
using OpenPay.Application.Interfaces;
using OpenPay.Domain.Entities;

namespace OpenPay.Infrastructure.Banking;

public class BankGatewayService : IBankGatewayService
{
    private readonly IBankAdapterRegistry _adapterRegistry;

    public BankGatewayService(IBankAdapterRegistry adapterRegistry)
    {
        _adapterRegistry = adapterRegistry;
    }

    public Task<BankSubmitResultDto> SubmitPaymentAsync(PaymentOrder payment)
    {
        var connection = GetConnection(payment);
        var adapter = _adapterRegistry.GetRequiredAdapter(connection.BankCode);
        return adapter.SubmitPaymentAsync(payment, connection);
    }

    public Task<BankStatusResultDto> CheckPaymentStatusAsync(PaymentOrder payment)
    {
        var connection = GetConnection(payment);
        var adapter = _adapterRegistry.GetRequiredAdapter(connection.BankCode);
        return adapter.CheckPaymentStatusAsync(payment, connection);
    }

    private static BankConnection GetConnection(PaymentOrder payment)
    {
        var connection = payment.OrganizationBankAccount?.BankConnection;

        if (connection == null)
            throw new InvalidOperationException("Для счета организации не настроено банковское подключение.");

        if (!connection.IsActive)
            throw new InvalidOperationException("Банковское подключение неактивно.");

        return connection;
    }
}
