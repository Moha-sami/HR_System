
namespace Buy2.Domain.Entities;

    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string PermissionsJson { get; set; } = string.Empty;
    }
