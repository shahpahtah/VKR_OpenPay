namespace OpenPay.Application.Common;

public class OrganizationInactiveException : InvalidOperationException
{
    public OrganizationInactiveException()
        : base("Организация текущего пользователя деактивирована.")
    {
    }
}