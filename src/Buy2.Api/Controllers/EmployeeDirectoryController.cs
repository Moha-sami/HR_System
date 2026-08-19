using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
