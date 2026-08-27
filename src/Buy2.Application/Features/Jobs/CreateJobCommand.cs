using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using System.Linq;

using Buy2.Application.Common.Security;

namespace Buy2.Application.Features.Jobs;

[Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
public record CreateJobCommand(CreateJobDto Dto) : IRequest<JobResponseDto>;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobResponseDto>
{
    private readonly IRepository<JobRole> _jobRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateJobCommandHandler(
        IRepository<JobRole> jobRepository,
        IRepository<Department> departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<JobResponseDto> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        ValidateWorkModel(dto);

        int departmentId = await ResolveDepartmentIdAsync(dto, cancellationToken);
        await EnsureTitleIsUniqueAsync(dto.Title, departmentId, cancellationToken);

        var job = new JobRole
        {
            Title = dto.Title,
            DepartmentId = departmentId,
            Description = dto.Description,
            SeniorityLevel = dto.SeniorityLevel,
            ExperienceYears = dto.ExperienceYearsMin,
            AttendanceType = dto.WorkModel,
            RequiredQualificationsJson = JsonSerializer.Serialize(dto.RequiredQualifications ?? new()),
            OnlineWorkdaysJson = JsonSerializer.Serialize(dto.OnlineWorkdays ?? new()),
            OfflineWorkdaysJson = JsonSerializer.Serialize(dto.OfflineWorkdays ?? new()),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _jobRepository.AddAsync(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new JobResponseDto(
            job.Id,
            job.Title,
            job.DepartmentId,
            null,
            job.SeniorityLevel,
            job.Description,
            dto.RequiredQualifications ?? new(),
            job.ExperienceYears,
            job.AttendanceType,
            dto.OnlineWorkdays ?? new(),
            dto.OfflineWorkdays ?? new(),
            job.IsActive,
            job.CreatedAt,
            job.UpdatedAt
        );
    }

    private void ValidateWorkModel(CreateJobDto dto)
    {
        if (dto.WorkModel == "OnSite" && dto.OnlineWorkdays?.Any() == true)
            throw new ArgumentException("OnSite work model cannot have online workdays.");
        
        if (dto.WorkModel == "Remote" && dto.OfflineWorkdays?.Any() == true)
            throw new ArgumentException("Remote work model cannot have offline workdays.");

        if (dto.WorkModel == "Hybrid")
        {
            int totalDays = (dto.OnlineWorkdays?.Count ?? 0) + (dto.OfflineWorkdays?.Count ?? 0);
            if (totalDays != 5)
                throw new ArgumentException("Hybrid work model total days must equal active work week (5 days).");
        }
    }

    private async Task<int> ResolveDepartmentIdAsync(CreateJobDto dto, CancellationToken cancellationToken)
    {
        if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
        {
            return dto.DepartmentId.Value;
        }
        
        if (!string.IsNullOrWhiteSpace(dto.NewDepartmentName))
        {
            var dept = new Department
            {
                Name = dto.NewDepartmentName,
                IsActive = true
            };
            await _departmentRepository.AddAsync(dept);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return dept.Id;
        }

        throw new ArgumentException("DepartmentId or NewDepartmentName is required.");
    }

    private async Task EnsureTitleIsUniqueAsync(string title, int departmentId, CancellationToken cancellationToken)
    {
        bool isDuplicate = await _jobRepository.AnyAsync(
            j => j.Title == title && j.DepartmentId == departmentId && j.IsActive, 
            cancellationToken);

        if (isDuplicate)
        {
            throw new InvalidOperationException("A job with the same title already exists in the department.");
        }
    }
}
