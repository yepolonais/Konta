namespace Konta.Domain.Budgets;

public sealed class MonthlyDisplaySectionPost
{
    private MonthlyDisplaySectionPost() { }

    public MonthlyDisplaySectionPost(Guid sectionId, Guid postId)
    {
        if (sectionId == Guid.Empty || postId == Guid.Empty)
        {
            throw new ArgumentException("La section et le poste sont requis.");
        }

        SectionId = sectionId;
        PostId = postId;
    }

    public Guid SectionId { get; private set; }
    public Guid PostId { get; private set; }
}
