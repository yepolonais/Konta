namespace Konta.Application.Imports;

public sealed class InvalidTransferMatchException(string message) : Exception(message);
