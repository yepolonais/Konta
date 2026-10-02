namespace Konta.Application.Accounts;

public sealed class DuplicateAccountException(string accountNumber)
    : Exception($"Le compte bancaire « {accountNumber} » est déjà associé.");