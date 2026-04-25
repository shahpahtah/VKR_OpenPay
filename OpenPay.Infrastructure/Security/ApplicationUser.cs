using Microsoft.AspNetCore.Identity;
using OpenPay.Domain.Entities;
using OpenPay.Domain.Enums;

namespace OpenPay.Infrastructure.Security;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Accountant;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public bool IsActive { get; set; } = true;
}