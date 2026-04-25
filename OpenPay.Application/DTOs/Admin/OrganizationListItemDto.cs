namespace OpenPay.Application.DTOs.Admin;

public class OrganizationListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string Kpp { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}