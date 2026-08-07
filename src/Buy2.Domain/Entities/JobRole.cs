namespace Buy2.Domain.Entities;

    public class JobRole : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PerformanceCriteria { get; set; }
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
