namespace Konta.Application.Imports;

public sealed record StatementBalance(string AccountNumber, DateOnly Date, decimal Balance);
