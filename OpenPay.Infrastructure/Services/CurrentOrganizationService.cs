using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Interfaces;
using OpenPay.Infrastructure.Persistence;

namespace OpenPay.Infrastructure.Services;

public class CurrentOrganizationService : ICurrentOrganizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OpenPayDbContext _dbContext;

    public CurrentOrganizationService(
        IHttpContextAccessor httpContextAccessor,
        OpenPayDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetCurrentOrganizationIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.OrganizationId)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> GetRequiredOrganizationIdAsync()
    {
        var organizationId = await GetCurrentOrganizationIdAsync();

        if (organizationId == null || organizationId == Guid.Empty)
            throw new InvalidOperationException("Для текущего пользователя не определена организация.");

        return organizationId.Value;
    }
}