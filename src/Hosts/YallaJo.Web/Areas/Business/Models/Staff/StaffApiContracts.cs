namespace YallaJo.Web.Areas.Business.Models.Staff;

public enum BusinessStaffRole : byte
{
    Owner = 0,
    Manager = 1,
    Receptionist = 2,
    Staff = 3
}

public sealed class BusinessStaffItemResponse
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public BusinessStaffRole Role { get; set; }
}

public sealed record AddBusinessStaffApiRequest(Guid UserId, BusinessStaffRole Role);
