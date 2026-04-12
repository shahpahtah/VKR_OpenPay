namespace OpenPay.Application.DTOs.Banking;

public class BankSubmitResultDto
{
    public bool IsAccepted { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}