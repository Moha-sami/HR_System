using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;

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
        var employee = await _employeeRepository.FirstOrDefaultAsync(
            e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        var spec = new Specification<EmployeeTask>()
            .Where(t => t.EmployeeId == request.EmployeeId);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusStr = request.Status.Trim();
            var statusClean = statusStr.Replace(" ", "").Replace("_", "").Replace("-", "");
            if (Enum.TryParse<EmployeeTaskStatus>(statusClean, ignoreCase: true, out var statusEnum))
            {
                spec.Where(t => t.Status == statusEnum);
            }
            else
            {
                return new List<EmployeeTaskDto>();
            }
        }

        spec.OrderBy(t => t.DueDate!).ThenBy(t => t.CreatedAt, descending: true);
        var tasks = await _taskRepository.ListAsync(spec, cancellationToken);

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
