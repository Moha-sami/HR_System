namespace Buy2.Domain.Entities;

    public class Employee : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty; 
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? NationalId { get; set; }
        public DateTime HireDate { get; set; }
        public Guid? JobRoleId { get; set; }
        public Guid? ManagerId { get; set; }
    }

