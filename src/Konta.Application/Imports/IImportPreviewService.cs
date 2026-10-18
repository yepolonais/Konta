namespace Konta.Application.Imports;

public interface IImportPreviewService
{
    Task<ImportPreview> PreviewAsync(Stream csvStream, CancellationToken cancellationToken);
    Task<ImportPreview> PreviewAsync(Stream csvStream, IReadOnlyDictionary<string, Guid>? accountMappings, CancellationToken cancellationToken);
    Task<ImportCommitResult> CommitAsync(Stream csvStream, CancellationToken cancellationToken);
    Task<ImportCommitResult> CommitAsync(Stream csvStream, IReadOnlyDictionary<string, Guid>? accountMappings, CancellationToken cancellationToken);
    Task<ImportCommitResult> CommitAsync(
        Stream csvStream,
        IReadOnlyDictionary<string, Guid>? accountMappings,
        IReadOnlyList<ImportTransactionReview>? review,
        CancellationToken cancellationToken);
}