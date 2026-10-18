namespace Konta.Application.Imports;

public sealed record TransferMatchConfirmation(Guid? TransactionId, int? RowNumber);
