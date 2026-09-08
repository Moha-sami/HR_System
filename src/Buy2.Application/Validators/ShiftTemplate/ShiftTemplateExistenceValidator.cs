using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.Validators;

public static class ShiftTemplateExistenceValidator
{
    public static async Task<Result> EnsureSitesExistAsync(
        IRepository<Site> siteRepository,
        List<int> siteIds,
        CancellationToken cancellationToken)
    {
        var count = await siteRepository.Query()
            .Where(s => siteIds.Contains(s.Id))
            .CountAsync(cancellationToken);

        if (count != siteIds.Count)
        {
            return Result.ValidationFailure("One or more SiteIds do not exist.");
        }

        return Result.Success();
    }

    public static async Task<Result> EnsureRolesExistAsync(
        IRepository<JobRole> jobRoleRepository,
        List<int> roleIds,
        CancellationToken cancellationToken)
    {
        var count = await jobRoleRepository.Query()
            .Where(r => roleIds.Contains(r.Id))
            .CountAsync(cancellationToken);

        if (count != roleIds.Count)
        {
            return Result.ValidationFailure("One or more JobRoleIds do not exist.");
        }

        return Result.Success();
    }

    public static async Task<Result> EnsureEmployeesExistAsync(
        IRepository<Employee> employeeRepository,
        List<int> employeeIds,
        CancellationToken cancellationToken)
    {
        var count = await employeeRepository.Query()
            .Where(e => employeeIds.Contains(e.Id))
            .CountAsync(cancellationToken);

        if (count != employeeIds.Count)
        {
            return Result.ValidationFailure("One or more AssignedUserIds do not exist.");
        }

        return Result.Success();
    }

    public static Result EnsureBlocksBelongToTemplate(
        Dictionary<int, ShiftBlock> existingById,
        List<int> incomingBlockIds)
    {
        foreach (var blockId in incomingBlockIds)
        {
            if (!existingById.ContainsKey(blockId))
            {
                return Result.NotFound($"Shift block with ID {blockId} was not found in this shift template.");
            }
        }

        return Result.Success();
    }
}
