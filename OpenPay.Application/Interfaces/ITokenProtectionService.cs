namespace OpenPay.Application.Interfaces;

public interface ITokenProtectionService
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}
