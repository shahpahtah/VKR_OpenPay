using Microsoft.AspNetCore.DataProtection;
using OpenPay.Application.Interfaces;

namespace OpenPay.Infrastructure.Services;

public class TokenProtectionService : ITokenProtectionService
{
    private readonly IDataProtector _protector;

    public TokenProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("OpenPay.BankTokens.v1");
    }

    public string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return _protector.Protect(value.Trim());
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
            return string.Empty;

        return _protector.Unprotect(protectedValue);
    }
}
