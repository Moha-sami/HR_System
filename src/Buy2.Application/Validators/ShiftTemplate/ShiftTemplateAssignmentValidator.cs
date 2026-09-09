using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.Validators;

public static class ShiftTemplateAssignmentValidator
{
    public static Result EnsureNoDuplicateEmployees(List<int> employeeIds)
    {
        var duplicate = employeeIds.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return Result.Conflict($"Employee {duplicate.Key} cannot be assigned to more than one block in the same shift.");
        }

        return Result.Success();
    }

    public static async Task<Result> EnsureEmployeesNotAssignedElsewhereAsync(
        IRepository<ShiftBlock> shiftBlockRepository,
        List<int> employeeIds,
        int? excludeTemplateId,
        CancellationToken cancellationToken)
    {
        var query = shiftBlockRepository.Query()
            .Where(b => b.EmployeeId.HasValue && employeeIds.Contains(b.EmployeeId.Value));
        if (excludeTemplateId.HasValue)
        {
            query = query.Where(b => b.ShiftTemplateId != excludeTemplateId.Value);
        }

        var taken = await query.Select(b => b.EmployeeId).Distinct().ToListAsync(cancellationToken);
        if (taken.Count > 0)
        {
            return Result.Conflict($"Employee(s) {string.Join(", ", taken)} are already assigned to another shift.");
        }

        return Result.Success();
    }
}
