namespace OpenPay.Application.DTOs.Organizations;

public class CurrentOrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
