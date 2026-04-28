using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Common;
using OpenPay.Application.DTOs.Organizations;
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

    public async Task<CurrentOrganizationDto?> GetCurrentOrganizationInfoAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && x.Organization != null)
            .Select(x => new CurrentOrganizationDto
            {
                Id = x.Organization!.Id,
                Name = x.Organization.Name,
                Inn = x.Organization.Inn,
                IsActive = x.Organization.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> GetRequiredOrganizationIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Не удалось определить текущего пользователя.");

        var result = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.OrganizationId,
                OrganizationIsActive = x.Organization != null ? x.Organization.IsActive : (bool?)null
            })
            .FirstOrDefaultAsync();

        if (result == null || result.OrganizationId == null || result.OrganizationId == Guid.Empty)
            throw new InvalidOperationException("Для текущего пользователя не определена организация.");

        if (result.OrganizationIsActive != true)
            throw new OrganizationInactiveException();

        return result.OrganizationId.Value;
    }
}
