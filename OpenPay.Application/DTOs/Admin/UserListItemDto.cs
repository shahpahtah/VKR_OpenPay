namespace OpenPay.Application.DTOs.Admin;

public class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public bool IsActive { get; set; }
}