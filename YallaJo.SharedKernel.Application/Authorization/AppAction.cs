namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Framework-agnostic verb constants used in permission declarations.
/// Every module uses the same verbs — therefore cross-cutting, lives in SharedKernel.
/// </summary>
public static class AppAction
{
    public const string Read       = nameof(Read);
    public const string Create     = nameof(Create);
    public const string Update     = nameof(Update);
    public const string Delete     = nameof(Delete);
    public const string UpdateSelf = nameof(UpdateSelf);
    public const string UpdateAny  = nameof(UpdateAny);
    public const string DeleteAny  = nameof(DeleteAny);
    public const string SoftDelete = nameof(SoftDelete);
    public const string Approve    = nameof(Approve);
    public const string Reject     = nameof(Reject);
    public const string Suspend    = nameof(Suspend);
    public const string Reinstate  = nameof(Reinstate);
    public const string Replay     = nameof(Replay);
}
