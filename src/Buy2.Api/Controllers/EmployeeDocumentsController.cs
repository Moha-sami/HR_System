using Buy2.Application.Features.Employees.UploadDocument;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees/{id}/documents")]
[Authorize]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly ISender _mediator;
    public EmployeeDocumentsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> Upload(int id, [FromBody] UploadEmployeeDocumentCommand command)
    {
        if (id != command.EmployeeId)
        {
            return BadRequest("Employee Id Does Not Match!");
        }
        var documentId = await _mediator.Send(command);

        return Ok(documentId);
    }
}