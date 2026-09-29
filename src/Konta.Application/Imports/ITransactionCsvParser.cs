namespace Konta.Application.Imports;

public interface ITransactionCsvParser
{
    ImportPreview Parse(Stream csvStream);
}