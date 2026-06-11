using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Business.Models.Staff;

public sealed class StaffVm
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string Status { get; set; } = "";
    public IReadOnlyList<StaffRowVm> Staff { get; set; } = [];
    public AddStaffFormVm Form { get; set; } = new();
    public bool HasStaff => Staff.Count > 0;
}

public sealed record StaffRowVm(
    Guid Id,
    Guid UserId,
    BusinessStaffRole Role,
    string? DisplayName = null,
    string? Email = null);

public sealed class AddStaffFormVm
{
    [Required]
    [Display(Name = "User ID")]
    public Guid UserId { get; set; }

    [Required]
    [Display(Name = "Role")]
    public BusinessStaffRole Role { get; set; } = BusinessStaffRole.Staff;
}
