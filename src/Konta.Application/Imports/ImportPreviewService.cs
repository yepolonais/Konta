namespace Konta.Application.Imports;

public sealed class ImportPreviewService(ITransactionCsvParser parser) : IImportPreviewService
{
    public ImportPreview Preview(Stream csvStream) => parser.Parse(csvStream);
}