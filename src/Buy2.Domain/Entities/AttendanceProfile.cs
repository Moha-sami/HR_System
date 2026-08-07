namespace Buy2.Domain.Entities
{
    public class AttendanceProfile : BaseEntity
    {
        public string ProfileName { get; set; } = string.Empty;
        public TimeSpan ExpectedClockIn { get; set; }
        public TimeSpan ExpectedClockOut { get; set; }
        public double RequiredWorkHours { get; set; }
    }
}