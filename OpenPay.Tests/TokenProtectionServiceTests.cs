using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using OpenPay.Infrastructure.Services;
using Xunit;

namespace OpenPay.Tests;

public class TokenProtectionServiceTests
{
    [Fact]
    public void Protect_should_round_trip_token_without_storing_plain_value()
    {
        var keyDirectory = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(),
            "openpay-token-tests",
            Guid.NewGuid().ToString("N")));

        var provider = DataProtectionProvider.Create(keyDirectory);
        var service = new TokenProtectionService(provider);

        var protectedValue = service.Protect("access-token");

        protectedValue.Should().NotBe("access-token");
        service.Unprotect(protectedValue).Should().Be("access-token");
    }
}
