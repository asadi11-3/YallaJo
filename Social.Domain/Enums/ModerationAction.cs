namespace Social.Domain.Enums;

/// <summary>Actions an admin can take when resolving a report or moderating content.</summary>
public enum ModerationAction : byte
{
    Dismiss         = 0,
    RemoveContent   = 1,
    WarnUser        = 2,
    BanUser         = 3,
    RestoreContent  = 4,
}
