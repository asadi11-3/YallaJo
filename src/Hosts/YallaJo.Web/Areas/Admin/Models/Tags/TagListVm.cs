namespace YallaJo.Web.Areas.Admin.Models.Tags;

public sealed class TagListVm
{
    public IReadOnlyList<TagRowVm> Tags { get; init; } = [];
    public CreateTagVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }

    /// <summary>When set, the Index renders the edit modal server-side open for this tag (PE1 deep link).</summary>
    public Guid? EditId { get; set; }

    /// <summary>Backing model for the Index-hosted edit modal (bound with prefix <c>Edit</c>).</summary>
    public UpdateTagVm Edit { get; set; } = new();
}
