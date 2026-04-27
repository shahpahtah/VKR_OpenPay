namespace OpenPay.Application.Common;

public static class BankingValidators
{
    private static readonly int[] Inn10Factors = [2, 4, 10, 3, 5, 9, 4, 6, 8];
    private static readonly int[] Inn12FirstFactors = [7, 2, 4, 10, 3, 5, 9, 4, 6, 8];
    private static readonly int[] Inn12SecondFactors = [3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8];
    private static readonly int[] AccountFactors = [7, 1, 3, 7, 1, 3, 7, 1, 3, 7, 1, 3, 7, 1, 3, 7, 1, 3, 7, 1, 3, 7, 1];

    public static bool IsDigitsOnly(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);

    public static bool IsValidInn(string? inn)
    {
        if (!IsDigitsOnly(inn))
            return false;

        inn = inn!.Trim();

        return inn.Length switch
        {
            10 => CalculateControlDigit(inn, Inn10Factors) == inn[9] - '0',
            12 => CalculateControlDigit(inn, Inn12FirstFactors) == inn[10] - '0' &&
                  CalculateControlDigit(inn, Inn12SecondFactors) == inn[11] - '0',
            _ => false
        };
    }

    public static bool IsValidKpp(string? kpp) =>
        IsDigitsOnly(kpp) && kpp!.Trim().Length == 9;

    public static bool IsValidBic(string? bic) =>
        IsDigitsOnly(bic) && bic!.Trim().Length == 9;

    public static bool IsValidSettlementAccount(string? bic, string? accountNumber)
    {
        if (!IsValidBic(bic) || !IsDigitsOnly(accountNumber) || accountNumber!.Trim().Length != 20)
            return false;

        var control = bic!.Trim()[^3..] + accountNumber.Trim();
        return IsValidAccountControl(control);
    }

    public static bool IsValidCorrespondentAccount(string? bic, string? correspondentAccount)
    {
        if (!IsValidBic(bic) || !IsDigitsOnly(correspondentAccount) || correspondentAccount!.Trim().Length != 20)
            return false;

        var normalizedBic = bic!.Trim();
        var control = "0" + normalizedBic.Substring(4, 2) + correspondentAccount.Trim();
        return IsValidAccountControl(control);
    }

    public static string? GetInnError(string? inn) =>
        IsValidInn(inn) ? null : "ИНН должен содержать 10 или 12 цифр и корректную контрольную сумму.";

    public static string? GetKppError(string? kpp) =>
        IsValidKpp(kpp) ? null : "КПП должен содержать 9 цифр.";

    public static string? GetBicError(string? bic) =>
        IsValidBic(bic) ? null : "БИК должен содержать 9 цифр.";

    public static string? GetSettlementAccountError(string? bic, string? accountNumber) =>
        IsValidSettlementAccount(bic, accountNumber)
            ? null
            : "Расчетный счет должен содержать 20 цифр и проходить проверку по БИК.";

    public static string? GetCorrespondentAccountError(string? bic, string? correspondentAccount) =>
        IsValidCorrespondentAccount(bic, correspondentAccount)
            ? null
            : "Корреспондентский счет должен содержать 20 цифр и проходить проверку по БИК.";

    private static int CalculateControlDigit(string value, IReadOnlyList<int> factors)
    {
        var sum = 0;

        for (var i = 0; i < factors.Count; i++)
            sum += (value[i] - '0') * factors[i];

        return sum % 11 % 10;
    }

    private static bool IsValidAccountControl(string control)
    {
        var sum = 0;

        for (var i = 0; i < AccountFactors.Length; i++)
            sum += (control[i] - '0') * AccountFactors[i];

        return sum % 10 == 0;
    }
}
