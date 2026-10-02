namespace Konta.Application.Imports;

public interface IImportPreviewService
{
    Task<ImportPreview> PreviewAsync(Stream csvStream, CancellationToken cancellationToken);
    Task<ImportCommitResult> CommitAsync(Stream csvStream, CancellationToken cancellationToken);
}