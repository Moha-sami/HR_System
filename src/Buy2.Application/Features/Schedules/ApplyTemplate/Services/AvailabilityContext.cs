using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class AvailabilityContext
{
    public HashSet<int> AuthorizedEmployeeIds { get; set; } = new();
    public bool IsDayOff { get; set; }
    public SiteOperationalHour? DayHours { get; set; }
    public List<Request> LeaveRequests { get; set; } = new();
    public HashSet<int> LeaveRecordEmployeeIds { get; set; } = new();
    public Dictionary<int, string> LeaveTypeByEmployeeId { get; set; } = new();
    public Dictionary<int, bool> RemoteWorkByEmployeeId { get; set; } = new();
}
