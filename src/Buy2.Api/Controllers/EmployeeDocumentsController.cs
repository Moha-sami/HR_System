using Buy2.Application.Features.Employees.UploadDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees/{id}/documents")]

public class EmployeeDocumentController : ControllerBase
{
    private readonly ISender _mediator;
    public EmployeeDocumentController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Upload(int id, UploadEmployeeDocumentCommand command)
    {
        if (id != command.EmployeeId)
        {
            return BadRequest("Employee Id Does Not Match!");
        }
        var documentId = await _mediator.Send(command);

        return Ok(documentId);
    }
}