using Microsoft.AspNetCore.Identity;

namespace Buy2.Domain.Entities;

    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
