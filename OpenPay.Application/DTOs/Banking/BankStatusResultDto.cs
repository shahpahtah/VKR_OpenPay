using OpenPay.Domain.Enums;

namespace OpenPay.Application.DTOs.Banking;

public class BankStatusResultDto
{
    public PaymentStatus FinalStatus { get; set; }
    public string Message { get; set; } = string.Empty;
}