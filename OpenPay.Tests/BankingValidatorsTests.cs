using FluentAssertions;
using OpenPay.Application.Common;
using Xunit;

namespace OpenPay.Tests;

public class BankingValidatorsTests
{
    [Theory]
    [InlineData("7707083893")]
    [InlineData("500100732259")]
    public void IsValidInn_should_accept_valid_control_sum(string inn)
    {
        BankingValidators.IsValidInn(inn).Should().BeTrue();
    }

    [Theory]
    [InlineData("7701234567")]
    [InlineData("123")]
    public void IsValidInn_should_reject_invalid_values(string inn)
    {
        BankingValidators.IsValidInn(inn).Should().BeFalse();
    }

    [Fact]
    public void IsValidSettlementAccount_should_check_bic_control_key()
    {
        BankingValidators.IsValidSettlementAccount("044525225", "40702810000000000007")
            .Should().BeTrue();

        BankingValidators.IsValidSettlementAccount("044525225", "40702810000000000001")
            .Should().BeFalse();
    }

    [Fact]
    public void IsValidCorrespondentAccount_should_check_bic_control_key()
    {
        BankingValidators.IsValidCorrespondentAccount("044525225", "30101810400000000225")
            .Should().BeTrue();

        BankingValidators.IsValidCorrespondentAccount("044525225", "30101810400000000221")
            .Should().BeFalse();
    }
}
