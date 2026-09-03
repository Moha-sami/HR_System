using Buy2.Application.DTOs;
using Buy2.Application.DTOs.Sites;
using Buy2.Application.Features.Sites.CreateSite;
using Buy2.Application.Features.Sites.DeleteSite;
using Buy2.Application.Features.Sites.Documents;
using Buy2.Application.Features.Sites.GetSiteDetails;
using Buy2.Application.Features.Sites.GetSites;
using Buy2.Application.Features.Sites.Regions;
using Buy2.Application.Features.Sites.UpdateSite;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;
[ApiController]
[Route("api/v1/sites")]
[Authorize]
public class GetSitesController : ControllerBase
{
    private readonly ISender _mediator;

    public GetSitesController(ISender mediator)
    {
        _mediator = mediator;
    }

    // Get All Sites
    [HttpGet]
    [ProducesResponseType(typeof(List<SiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SiteDto>>> GetSites(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSitesQuery(), cancellationToken);
        return Ok(result);
    }

    // Add New Site
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> CreateSite(CreateUpdateSiteDto dto, CancellationToken cancellation)
    {
        var command = new CreateSiteCommand(
            dto.SiteName, dto.Latitude, dto.Longitude, dto.MacWhitelist, dto.MacAddress, 
            dto.Address, dto.MapUrl, dto.PhoneNumber,  dto.Instructions,
            dto.RegionId, dto.MaxCapacity, dto.PreferredEmployeeIds, dto.OperationalHours
        );
        var site = await _mediator.Send(command, cancellation);
        return Created($"/api/v1/sites/{site}", site);
    }

    // Update Site
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> UpdateSite(int id, CreateUpdateSiteDto dto, CancellationToken cancellation)
    {
        var command = new UpdateSiteCommand(
            id, dto.SiteName, dto.Latitude, dto.Longitude, dto.MacWhitelist,
            dto.MacAddress, dto.Address, dto.MapUrl, dto.PhoneNumber, dto.Instructions,
            dto.RegionId, dto.MaxCapacity, dto.PreferredEmployeeIds, dto.OperationalHours
        );
        var site = await _mediator.Send(command, cancellation);
        return Ok(site);
    }

    // Deletion Check
    [HttpGet("{id}/deletion-check")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeletionCheckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeletionCheckDto>> GetDeletionCheck(int id, CancellationToken cancellation)
    {
        var site = await _mediator.Send(new CheckSiteDeletionQuery(id), cancellation);
        return Ok(site);
    }
    // Delete Site
    [HttpDelete("{id}")]
    [Authorize(Roles ="Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSite(int id, [FromBody] ReallocateAndDeleteSiteDto? dto, CancellationToken cancellation)
    {
        var command = new DeleteSiteCommand(id, dto?.EmployeeSiteReassignments);
        await _mediator.Send(command, cancellation);
        return NoContent();
    }

    // Get Site Info
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SiteFullProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteFullProfile>> GetSiteFullProfileInfo(int id, CancellationToken cancellation)
    {
        var site = await _mediator.Send(new GetSiteBasicInfoQuery(id), cancellation);
        return Ok(site);
    }

    // Get Sits Shifts
    [HttpGet("{id}/shifts")]
    [ProducesResponseType(typeof(List<ShiftClaimsController>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ShiftTabDto>>> GetSiteShifts(int id, CancellationToken cancellation)
    {
        var shifts = await _mediator.Send(new GetSiteShiftsQuery(id), cancellation);
        return Ok(shifts);
    }

    // Get Sits Employees
    [HttpGet("{id}/employees")]
    [ProducesResponseType(typeof(List<EmployeeTabDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EmployeeTabDto>>> GetSiteEmployees(int id, CancellationToken cancellation)
    {
        var employees = await _mediator.Send(new GetSiteEmployeesQuery(id), cancellation);
        return Ok(employees);
    }

    // Get All Regions
    [HttpGet("regions")]
    [ProducesResponseType(typeof(List<RegionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RegionListItemDto>>> GetRegions(CancellationToken cancellation)
    {
        var regions = await _mediator.Send(new GetRegionsQuery(), cancellation);
        return Ok(regions);
    }

    // Create New Region
    [HttpPost("regions")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> CreateRegion(CreateRegionDto dto, CancellationToken cancellation)
    {
        var command = new CreateRegionCommand(Name: dto.Name);
        var region = await _mediator.Send(command, cancellation);
        return Created($"/api/v1/sites/regions/{region}", region);
    }

    // Upload Documents
    [HttpPost("{id}/documents")]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentDto>> UploadDocument(int id, [FromForm] IFormFile file, CancellationToken cancellation)
    {
        var command = new UploadSiteDocumentCommand(id, file);
        var document = await _mediator.Send(command, cancellation);
        return Created($"/api/v1/sites/{id}/documents/{document.Id}", document);
    }

    // Delete Document
    [HttpDelete("{id}/documents/{documentId}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteDocument(int id, int documentId, CancellationToken cancellation)
    {
        var command = new DeleteSiteDocumentCommand(id, documentId);
        await _mediator.Send(command, cancellation);
        return NoContent();
    }
}
