using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Contracts.Authorization
{
    public static class AppFeatures
    {
        public const string Role = nameof(Role);
        public const string UserRole = nameof(UserRole);
        public const string RoleClaim = nameof(RoleClaim);
        public const string System = nameof(System);
        public const string User = nameof(User);
        public const string Category = nameof(Category);
        public const string CategoryTranslation = nameof(CategoryTranslation);
        public const string Specialization = nameof(Specialization);
        public const string Tag = nameof(Tag);
        public const string EntityCategory = nameof(EntityCategory);
        public const string EntityImage = nameof(EntityImage);
        public const string EntityTag = nameof(EntityTag);
        public const string TranslationCache = nameof(TranslationCache);
        public const string Language = nameof(Language);
        public const string Attachment = nameof(Attachment);
        public const string Place = nameof(Place);
        public const string Booking = nameof(Booking);
        public const string BusinessStaff = nameof(BusinessStaff);
        public const string BusinessAmenity = nameof(BusinessAmenity);
        public const string AccessibilityFeature = nameof(AccessibilityFeature);
    }
}
