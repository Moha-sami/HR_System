using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.DeleteEmployee;
using Buy2.Application.Features.Employees.ExportEmployees;
using Buy2.Application.Features.Employees.GetAttendanceCalendar;
using Buy2.Application.Features.Employees.GetEmployee;
using Buy2.Application.Features.Employees.GetEmployeePayroll;
using Buy2.Application.Features.Employees.GetEmployeeTasks;
using Buy2.Application.Features.Employees.GetEmployees;
using Buy2.Application.Features.Employees.GetMetricDetail;
using Buy2.Application.Features.Employees.GetPerformanceOverview;
using Buy2.Application.Features.Employees.GetPointsSummary;
using Buy2.Application.Features.Employees.UpdateJobDetails;
using Buy2.Application.Features.Employees.UpdatePayrollProfile;
using Buy2.Application.Features.Employees.UpdatePersonalInfo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetricDetailDto = Buy2.Application.Features.Employees.GetMetricDetail.MetricDetailDto;
using PerformanceOverviewDto = Buy2.Application.Features.Employees.GetPerformanceOverview.PerformanceOverviewDto;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeeDirectoryController : ControllerBase
{
    private readonly ISender _mediator;

    public EmployeeDirectoryController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedEmployeeListDto>> GetEmployees([FromQuery] GetEmployeesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployeeProfileDto>> GetEmployeeProfile(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEmployeeProfileQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("{id:int}/payroll")]
    [Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]
    [ProducesResponseType(typeof(EmployeePayrollProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeePayrollProfileDto>> GetEmployeePayrollProfile(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEmployeePayrollProfileQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpPut("{id:int}/payroll")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateEmployeePayrollProfile(int id, [FromBody] UpdatePayrollProfileDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdatePayrollProfileCommand(id, dto), cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }
        return NoContent();
    }

    [HttpGet("export")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportEmployees([FromQuery] ExportEmployeesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return File(result, "text/csv", "employees.csv");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteEmployeeCommand(id), cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{id:int}/personal")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateEmployeePersonalInfo(int id, [FromBody] UpdateEmployeePersonalInfoDto dto, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateEmployeePersonalInfoCommand(id, dto), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{id:int}/job")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateEmployeeJobDetails(int id, [FromBody] UpdateJobDetailsDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateJobDetailsCommand(id, dto), cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }
        return NoContent();
    }

    private static readonly HashSet<string> ValidPeriods = new(StringComparer.OrdinalIgnoreCase)
    {
        "today", "thisweek", "week", "thismonth", "month", "thisyear", "year"
    };

    [HttpGet("{id:int}/performance/overview")]
    [Authorize]
    [ProducesResponseType(typeof(PerformanceOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PerformanceOverviewDto>> GetPerformanceOverview(
        [FromRoute] int id,
        [FromQuery] string? period,
        [FromQuery] int? days,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(period) && !ValidPeriods.Contains(period.Trim()))
        {
            return BadRequest($"Invalid period '{period}'. Supported values: today, thisWeek, thisMonth, thisYear.");
        }

        if (days.HasValue && (days.Value <= 0 || days.Value > 3650))
        {
            return BadRequest("Days parameter must be between 1 and 3650.");
        }

        if (from.HasValue && to.HasValue && Math.Abs((to.Value - from.Value).TotalDays) > 3650)
        {
            return BadRequest("Date range cannot exceed 3650 days (10 years).");
        }

        var result = await _mediator.Send(new GetPerformanceOverviewQuery(id, period, days, from, to), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("{id:int}/performance/metrics/{metricId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(MetricDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MetricDetailDto>> GetMetricDetail(
        [FromRoute] int id,
        [FromRoute] int metricId,
        [FromQuery] string? period,
        [FromQuery] int? days,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(period) && !ValidPeriods.Contains(period.Trim()))
        {
            return BadRequest($"Invalid period '{period}'. Supported values: today, thisWeek, thisMonth, thisYear.");
        }

        if (days.HasValue && (days.Value <= 0 || days.Value > 3650))
        {
            return BadRequest("Days parameter must be between 1 and 3650.");
        }

        if (from.HasValue && to.HasValue && Math.Abs((to.Value - from.Value).TotalDays) > 3650)
        {
            return BadRequest("Date range cannot exceed 3650 days (10 years).");
        }

        var result = await _mediator.Send(new GetMetricDetailQuery(id, metricId, period, days, from, to), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("{id:int}/tasks")]
    [Authorize]
    [ProducesResponseType(typeof(List<EmployeeTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EmployeeTaskDto>>> GetEmployeeTasks(
        [FromRoute] int id,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEmployeeTasksQuery(id, status), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("{id:int}/attendance/calendar")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceCalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AttendanceCalendarDto>> GetAttendanceCalendar(
        [FromRoute] int id,
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAttendanceCalendarQuery(id, month, year), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("{id:int}/points/summary")]
    [Authorize]
    [ProducesResponseType(typeof(PointsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PointsSummaryDto>> GetPointsSummary(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPointsSummaryQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }
}


