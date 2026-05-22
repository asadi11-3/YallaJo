namespace Analytics.Domain.Enums;

public enum AuditLogAction : byte
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Approve = 4,
    Reject = 5,
    Confirm = 6,
    Cancel = 7,
    Refund = 8,
    Login = 9,
    Logout = 10,
    PasswordChange = 11,
    PermissionGrant = 12,
    PermissionRevoke = 13,
    Custom = 99
}
