using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.UpdateShiftTemplate;

public sealed record ParsedBlockTime(
    int BlockId,
    TimeSpan Start,
    TimeSpan End,
    int JobRoleId,
    int EmployeeId);

public sealed record ParsedTemplateTimes(
    TimeSpan TemplateStart,
    TimeSpan TemplateEnd,
    List<ParsedBlockTime> Blocks);

public sealed record ValidatedShiftTemplateUpdate(
    ShiftTemplate Template,
    string Name,
    TimeSpan TemplateStart,
    TimeSpan TemplateEnd,
    List<int> SiteIds,
    List<ParsedBlockTime> Blocks);

public class ShiftTemplateUpdateService
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IRepository<ShiftTemplateSite> _shiftTemplateSiteRepository;
    private readonly IRepository<ShiftBlock> _shiftBlockRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public ShiftTemplateUpdateService(
        IRepository<ShiftTemplate> shiftTemplateRepository,
        IRepository<ShiftTemplateSite> shiftTemplateSiteRepository,
        IRepository<ShiftBlock> shiftBlockRepository,
        IRepository<Site> siteRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<Employee> employeeRepository)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
        _shiftTemplateSiteRepository = shiftTemplateSiteRepository;
        _shiftBlockRepository = shiftBlockRepository;
        _siteRepository = siteRepository;
        _jobRoleRepository = jobRoleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<ValidatedShiftTemplateUpdate>> ValidateAsync(
        int templateId,
        UpdateShiftTemplateDto dto,
        CancellationToken cancellationToken)
    {
        var template = await _shiftTemplateRepository.Query(asNoTracking: false)
            .Include(t => t.ShiftTemplateSites)
            .Include(t => t.ShiftBlocks)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template is null)
        {
            return Result<ValidatedShiftTemplateUpdate>.NotFound(
                $"Shift template with ID {templateId} was not found.");
        }

        var blockSet = EnsureBlockSetMatches(template, dto);
        if (!blockSet.IsSuccess)
        {
            return Propagate<ValidatedShiftTemplateUpdate>(blockSet);
        }

        var times = ValidateTimes(dto);
        if (!times.IsSuccess)
        {
            return Result<ValidatedShiftTemplateUpdate>.ValidationFailure(
                times.ErrorMessage ?? "Invalid time format.");
        }

        var references = await EnsureReferencesExistAsync(dto, cancellationToken);
        if (!references.IsSuccess)
        {
            return Propagate<ValidatedShiftTemplateUpdate>(references);
        }

        var assignments = await EnsureAssignmentsValidAsync(template.Id, dto, cancellationToken);
        if (!assignments.IsSuccess)
        {
            return Propagate<ValidatedShiftTemplateUpdate>(assignments);
        }

        return Result<ValidatedShiftTemplateUpdate>.Success(new ValidatedShiftTemplateUpdate(
            template,
            dto.Name.Trim(),
            times.Value!.TemplateStart,
            times.Value.TemplateEnd,
            dto.SiteIds.Distinct().ToList(),
            times.Value.Blocks));
    }

    public void Apply(ValidatedShiftTemplateUpdate validated, int? actorEmployeeId)
    {
        var template = validated.Template;
        template.Name = validated.Name;
        template.StartTime = validated.TemplateStart;
        template.EndTime = validated.TemplateEnd;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        template.LastUpdatedByEmployeeId = actorEmployeeId;

        ReplaceSiteLinks(template, validated.SiteIds);
        SyncBlocks(template, validated.Blocks);
    }

    private static Result EnsureBlockSetMatches(ShiftTemplate template, UpdateShiftTemplateDto dto)
    {
        var seenIds = new HashSet<int>();

        foreach (var block in dto.ShiftBlocks)
        {
            if (block.Id < 0)
            {
                return Result.ValidationFailure(
                    $"Shift block ID {block.Id} is invalid. Use 0 for new blocks.");
            }

            if (block.Id == 0)
            {
                continue;
            }

            if (!seenIds.Add(block.Id))
            {
                return Result.ValidationFailure(
                    $"Duplicate shift block ID {block.Id} is not allowed.");
            }
        }

        return ShiftTemplateExistenceValidator.EnsureBlocksBelongToTemplate(
            template.ShiftBlocks.ToDictionary(b => b.Id),
            dto.ShiftBlocks.Where(b => b.Id > 0).Select(b => b.Id).ToList());
    }

    private static Result<ParsedTemplateTimes> ValidateTimes(UpdateShiftTemplateDto dto)
    {
        var parsed = ParseTimes(dto);
        if (!parsed.IsSuccess)
        {
            return Result<ParsedTemplateTimes>.ValidationFailure(
                parsed.ErrorMessage ?? "Invalid time format.");
        }

        var rules = EnsureTimeRules(parsed.Value!);
        if (!rules.IsSuccess)
        {
            return Result<ParsedTemplateTimes>.ValidationFailure(
                rules.ErrorMessage ?? "Invalid shift block time.");
        }

        return Result<ParsedTemplateTimes>.Success(parsed.Value!);
    }

    private static Result<ParsedTemplateTimes> ParseTimes(UpdateShiftTemplateDto dto)
    {
        if (!ShiftTimeHelper.TryParseTime(dto.StartTime, out var templateStart)
            || !ShiftTimeHelper.TryParseTime(dto.EndTime, out var templateEnd))
        {
            return Result<ParsedTemplateTimes>.ValidationFailure(
                "StartTime and EndTime must be valid times in 'hh:mm tt' format (e.g. '09:00 AM').");
        }

        var blocks = new List<ParsedBlockTime>(dto.ShiftBlocks.Count);
        foreach (var block in dto.ShiftBlocks)
        {
            if (!ShiftTimeHelper.TryParseTime(block.StartTime, out var blockStart)
                || !ShiftTimeHelper.TryParseTime(block.EndTime, out var blockEnd))
            {
                var label = block.Id > 0 ? $"Shift block {block.Id}" : "New shift block";
                return Result<ParsedTemplateTimes>.ValidationFailure(
                    $"{label} has invalid StartTime or EndTime. " +
                    "Use the 'hh:mm tt' format (e.g. '09:00 AM').");
            }

            blocks.Add(new ParsedBlockTime(block.Id, blockStart, blockEnd, block.JobRoleId, block.AssignedUserId));
        }

        return Result<ParsedTemplateTimes>.Success(new ParsedTemplateTimes(templateStart, templateEnd, blocks));
    }

    private static Result EnsureTimeRules(ParsedTemplateTimes parsed)
    {
        foreach (var block in parsed.Blocks)
        {
            if (!ShiftTimeHelper.IsWithinTemplate(block.Start, block.End, parsed.TemplateStart, parsed.TemplateEnd))
            {
                var label = block.BlockId > 0 ? $"Shift block {block.BlockId}" : "New shift block";
                return Result.ValidationFailure(
                    $"{label} must be within the template time.");
            }
        }

        var intervals = parsed.Blocks.Select(b => (b.Start, b.End)).ToList();
        if (ShiftTimeHelper.HasOverlappingBlocks(intervals, parsed.TemplateStart))
        {
            return Result.ValidationFailure("Shift blocks must not overlap.");
        }

        return Result.Success();
    }

    private async Task<Result> EnsureReferencesExistAsync(
        UpdateShiftTemplateDto dto,
        CancellationToken cancellationToken)
    {
        var siteIds = dto.SiteIds.Distinct().ToList();
        var sitesCheck = await ShiftTemplateExistenceValidator.EnsureSitesExistAsync(
            _siteRepository, siteIds, cancellationToken);
        if (!sitesCheck.IsSuccess)
        {
            return sitesCheck;
        }

        var roleIds = dto.ShiftBlocks.Select(b => b.JobRoleId).Distinct().ToList();
        var rolesCheck = await ShiftTemplateExistenceValidator.EnsureRolesExistAsync(
            _jobRoleRepository, roleIds, cancellationToken);
        if (!rolesCheck.IsSuccess)
        {
            return rolesCheck;
        }

        var employeeIds = dto.ShiftBlocks.Select(b => b.AssignedUserId).Distinct().ToList();
        return await ShiftTemplateExistenceValidator.EnsureEmployeesExistAsync(
            _employeeRepository, employeeIds, cancellationToken);
    }

    private async Task<Result> EnsureAssignmentsValidAsync(
        int templateId,
        UpdateShiftTemplateDto dto,
        CancellationToken cancellationToken)
    {
        var duplicateCheck = ShiftTemplateAssignmentValidator.EnsureNoDuplicateEmployees(
            dto.ShiftBlocks.Select(b => b.AssignedUserId).ToList());
        if (!duplicateCheck.IsSuccess)
        {
            return duplicateCheck;
        }

        var employeeIds = dto.ShiftBlocks.Select(b => b.AssignedUserId).Distinct().ToList();
        return await ShiftTemplateAssignmentValidator.EnsureEmployeesNotAssignedElsewhereAsync(
            _shiftBlockRepository, employeeIds, excludeTemplateId: templateId, cancellationToken);
    }

    private void ReplaceSiteLinks(ShiftTemplate template, List<int> siteIds)
    {
        var wanted = siteIds.ToHashSet();

        var toRemove = template.ShiftTemplateSites
            .Where(s => !wanted.Contains(s.SiteId))
            .ToList();

        foreach (var link in toRemove)
        {
            _shiftTemplateSiteRepository.Delete(link);
            template.ShiftTemplateSites.Remove(link);
        }

        var existingSiteIds = template.ShiftTemplateSites.Select(s => s.SiteId).ToHashSet();

        foreach (var siteId in siteIds)
        {
            if (existingSiteIds.Contains(siteId))
            {
                continue;
            }

            template.ShiftTemplateSites.Add(new ShiftTemplateSite
            {
                ShiftTemplateId = template.Id,
                SiteId = siteId
            });
        }
    }

    private void SyncBlocks(ShiftTemplate template, List<ParsedBlockTime> blocks)
    {
        var existingById = template.ShiftBlocks.ToDictionary(b => b.Id);
        var incomingIds = blocks
            .Where(b => b.BlockId > 0)
            .Select(b => b.BlockId)
            .ToHashSet();

        var toRemove = template.ShiftBlocks
            .Where(b => !incomingIds.Contains(b.Id))
            .ToList();

        foreach (var doomed in toRemove)
        {
            _shiftBlockRepository.Delete(doomed);
            template.ShiftBlocks.Remove(doomed);
        }

        foreach (var parsed in blocks)
        {
            if (parsed.BlockId == 0)
            {
                template.ShiftBlocks.Add(new ShiftBlock
                {
                    ShiftTemplateId = template.Id,
                    StartTime = parsed.Start,
                    EndTime = parsed.End,
                    JobRoleId = parsed.JobRoleId,
                    EmployeeId = parsed.EmployeeId
                });

                continue;
            }

            var existing = existingById[parsed.BlockId];
            existing.StartTime = parsed.Start;
            existing.EndTime = parsed.End;
            existing.JobRoleId = parsed.JobRoleId;
            existing.EmployeeId = parsed.EmployeeId;
        }
    }

    private static Result<T> Propagate<T>(Result source) => source.ErrorType switch
    {
        ResultErrorType.NotFound => Result<T>.NotFound(source.ErrorMessage ?? "Related record was not found."),
        ResultErrorType.Conflict => Result<T>.Conflict(source.ErrorMessage ?? "Related record is in conflict."),
        _ => Result<T>.ValidationFailure(source.ErrorMessage ?? "Validation failed.")
    };
}
