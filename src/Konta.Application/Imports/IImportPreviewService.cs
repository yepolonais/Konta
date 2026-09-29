namespace Konta.Application.Imports;

public interface IImportPreviewService
{
    ImportPreview Preview(Stream csvStream);
}