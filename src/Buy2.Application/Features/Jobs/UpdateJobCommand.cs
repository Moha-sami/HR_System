using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using System.Linq;
using System.Collections.Generic;

namespace Buy2.Application.Features.Jobs;

public record UpdateJobCommand(int Id, UpdateJobDto Dto) : IRequest<JobResponseDto>;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, JobResponseDto>
{
    private readonly IRepository<JobRole> _jobRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobCommandHandler(
        IRepository<JobRole> jobRepository,
        IRepository<Department> departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<JobResponseDto> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.Id);
        if (job == null)
            throw new KeyNotFoundException("Job not found.");

        var dto = request.Dto;

        ValidateWorkModel(dto);

        int? targetDepartmentId = dto.DepartmentId ?? job.DepartmentId;
        if (dto.Title != job.Title || targetDepartmentId != job.DepartmentId)
        {
            if (targetDepartmentId.HasValue)
            {
                await EnsureTitleIsUniqueAsync(dto.Title, targetDepartmentId.Value, request.Id, cancellationToken);
            }
        }

        job.Title = dto.Title;
        if (dto.DepartmentId.HasValue)
        {
            job.DepartmentId = dto.DepartmentId.Value;
        }
        job.SeniorityLevel = dto.SeniorityLevel;
        job.Description = dto.Description;
        job.RequiredQualificationsJson = JsonSerializer.Serialize(dto.RequiredQualifications ?? new());
        job.ExperienceYears = dto.ExperienceYearsMin;
        job.AttendanceType = dto.WorkModel;
        job.OnlineWorkdaysJson = JsonSerializer.Serialize(dto.OnlineWorkdays ?? new());
        job.OfflineWorkdaysJson = JsonSerializer.Serialize(dto.OfflineWorkdays ?? new());
        job.IsActive = dto.IsActive;
        job.UpdatedAt = DateTimeOffset.UtcNow;

        _jobRepository.Update(job);
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

    private void ValidateWorkModel(UpdateJobDto dto)
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

    private async Task EnsureTitleIsUniqueAsync(string title, int departmentId, int currentJobId, CancellationToken cancellationToken)
    {
        bool isDuplicate = await _jobRepository.AnyAsync(
            j => j.Title == title && j.DepartmentId == departmentId && j.Id != currentJobId && j.IsActive, 
            cancellationToken);

        if (isDuplicate)
        {
            throw new InvalidOperationException("A job with the same title already exists in the department.");
        }
    }
}
