using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetEmployeeTasks;

public record GetEmployeeTasksQuery(
    int EmployeeId,
    string? Status = null
) : IRequest<List<EmployeeTaskDto>?>;

public class GetEmployeeTasksQueryHandler : IRequestHandler<GetEmployeeTasksQuery, List<EmployeeTaskDto>?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeTask> _taskRepository;

    public GetEmployeeTasksQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeTask> taskRepository)
    {
        _employeeRepository = employeeRepository;
        _taskRepository = taskRepository;
    }

    public async Task<List<EmployeeTaskDto>?> Handle(GetEmployeeTasksQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        var query = _taskRepository.Query()
            .Where(t => t.EmployeeId == request.EmployeeId);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusStr = request.Status.Trim();
            var statusClean = statusStr.Replace(" ", "").Replace("_", "").Replace("-", "");
            if (Enum.TryParse<EmployeeTaskStatus>(statusClean, ignoreCase: true, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }
            else
            {
                return new List<EmployeeTaskDto>();
            }
        }

        var tasks = await query
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tasks.Select(t => new EmployeeTaskDto(
            Id: t.Id,
            EmployeeId: t.EmployeeId,
            Title: t.TaskName,
            Description: string.IsNullOrWhiteSpace(t.Description) ? null : t.Description,
            Status: t.Status.ToString(),
            Priority: null,
            DueDate: t.DueDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(t.DueDate.Value, DateTimeKind.Utc)) : null,
            CompletedAt: null,
            CreatedAt: new DateTimeOffset(DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc))
        )).ToList();
    }
}
