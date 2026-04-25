using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenPay.Infrastructure.Persistence;
using Xunit;

namespace OpenPay.Tests;

public class MultiOrganizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MultiOrganizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seeded_counterparties_should_belong_to_different_organizations()
    {
        await TestDataSeeder.SeedAsync(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenPayDbContext>();

        var allCounterparties = db.Counterparties.ToList();

        allCounterparties.Should().HaveCount(2);
        allCounterparties.Select(x => x.OrganizationId).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Counterparties_should_be_stored_with_organization_id()
    {
        await TestDataSeeder.SeedAsync(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenPayDbContext>();

        var alpha = db.Counterparties.Single(x => x.FullName == "Контрагент Альфа");
        var beta = db.Counterparties.Single(x => x.FullName == "Контрагент Бета");

        alpha.OrganizationId.Should().NotBeEmpty();
        beta.OrganizationId.Should().NotBeEmpty();
        alpha.OrganizationId.Should().NotBe(beta.OrganizationId);
    }
}