using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.DeleteDocument;
using Buy2.Application.Features.Employees.GetDocuments;
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

    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EmployeeDocumentDto>>> GetDocuments(int id, CancellationToken ct)
    {
        var documents = await _mediator.Send(new GetEmployeeDocumentsQuery(id), ct);
        return Ok(documents);
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> Upload(int id, [FromBody] UploadEmployeeDocumentCommand command, CancellationToken cancellationToken)
    {
        if (command.EmployeeId != 0 && id != command.EmployeeId)
        {
            return BadRequest("Employee Id Does Not Match!");
        }

        var cmd = command.EmployeeId == 0 ? command with { EmployeeId = id } : command;
        var documentId = await _mediator.Send(cmd, cancellationToken);

        return Ok(documentId);
    }

    [HttpDelete("{documentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteDocument(int id, int documentId, CancellationToken ct)
    {
        var deleted = await _mediator.Send(new DeleteEmployeeDocumentCommand(id, documentId), ct);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}