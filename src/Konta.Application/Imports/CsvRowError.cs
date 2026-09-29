namespace Konta.Application.Imports;

public sealed record CsvRowError(int RowNumber, string Message);