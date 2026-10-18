namespace Konta.Application.Imports;

public sealed class InvalidImportReviewException(IReadOnlyList<string> errors)
    : Exception("Certaines corrections de l'import sont invalides.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
