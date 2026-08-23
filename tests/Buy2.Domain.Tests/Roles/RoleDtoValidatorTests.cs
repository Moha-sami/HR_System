using Buy2.Application.DTOs.Roles;
using Buy2.Application.Validators;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class RoleDtoValidatorTests
{
    private readonly CreateRoleDtoValidator _createValidator = new();
    private readonly UpdateRoleDtoValidator _updateValidator = new();

    [Fact]
    public void CreateRoleDtoValidator_ValidPayload_ShouldPassValidation()
    {
        var dto = new CreateRoleDto(
            Name: "  HR Administrator  ",
            Description: "Full HR management permissions",
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto(
                    Module: "EmployeeManagement",
                    Actions: new List<string> { "Add", "Edit", "Suspend" },
                    Scope: new PermissionScopeDto("All", null)
                ),
                new ModulePermissionDto(
                    Module: "SiteManagement",
                    Actions: new List<string> { "ManageShifts" },
                    Scope: new PermissionScopeDto("Site", new List<int> { 101, 102 })
                )
            }
        );

        var result = _createValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void UpdateRoleDtoValidator_ValidPayload_ShouldPassValidation()
    {
        var dto = new UpdateRoleDto(
            Name: "Site Supervisor",
            Description: null,
            IsActive: true,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto(
                    Module: "SiteManagement",
                    Actions: new List<string> { "ManageShifts", "Edit" },
                    Scope: new PermissionScopeDto("Site", new List<int> { 5 })
                )
            }
        );

        var result = _updateValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // Length 1 after trim (< 2)
    public void Validators_InvalidNames_ShouldFail(string? invalidName)
    {
        var createDto = new CreateRoleDto(
            Name: invalidName!,
            Description: "Valid desc",
            Permissions: CreateValidPermissions()
        );

        var updateDto = new UpdateRoleDto(
            Name: invalidName!,
            Description: "Valid desc",
            IsActive: true,
            Permissions: CreateValidPermissions()
        );

        var createResult = _createValidator.Validate(createDto);
        var updateResult = _updateValidator.Validate(updateDto);

        Assert.False(createResult.IsValid);
        Assert.False(updateResult.IsValid);
    }

    [Fact]
    public void Validators_NameExceeds100Characters_ShouldFail()
    {
        var longName = new string('X', 101);
        var dto = new CreateRoleDto(longName, "Desc", CreateValidPermissions());

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRoleDto.Name));
    }

    [Fact]
    public void Validators_DescriptionExceeds500Characters_ShouldFail()
    {
        var longDesc = new string('D', 501);
        var dto = new CreateRoleDto("Valid Role Name", longDesc, CreateValidPermissions());

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRoleDto.Description));
    }

    [Fact]
    public void Validators_NullOrEmptyPermissions_ShouldFail()
    {
        var nullPermissionsDto = new CreateRoleDto("Role Name", "Desc", null);
        var emptyPermissionsDto = new CreateRoleDto("Role Name", "Desc", new List<ModulePermissionDto>());

        var nullResult = _createValidator.Validate(nullPermissionsDto);
        var emptyResult = _createValidator.Validate(emptyPermissionsDto);

        Assert.False(nullResult.IsValid);
        Assert.False(emptyResult.IsValid);
    }

    [Fact]
    public void Validators_DuplicateModuleEntries_ShouldFail()
    {
        var dto = new CreateRoleDto(
            Name: "Duplicate Modules Role",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("EmployeeManagement", new List<string> { "Add" }, new PermissionScopeDto("All", null)),
                new ModulePermissionDto("employeemanagement", new List<string> { "Edit" }, new PermissionScopeDto("All", null))
            }
        );

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("duplicate module entries", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validators_UnsupportedActionForModule_ShouldFail()
    {
        var dto = new CreateRoleDto(
            Name: "Unsupported Action Role",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("EmployeeManagement", new List<string> { "Add", "SuperAdminNuke" }, new PermissionScopeDto("All", null))
            }
        );

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("supported for the specified module", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validators_InvalidModuleName_ShouldFail()
    {
        var dto = new CreateRoleDto(
            Name: "Invalid Module Role",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("NonExistentModule", new List<string> { "Add" }, new PermissionScopeDto("All", null))
            }
        );

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("valid PermissionModule enum value name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validators_InvalidScopeTypeName_ShouldFail()
    {
        var dto = new CreateRoleDto(
            Name: "Invalid Scope Role",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("EmployeeManagement", new List<string> { "Add" }, new PermissionScopeDto("SuperScope", new List<int> { 1 }))
            }
        );

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("valid AccessScope enum value name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validators_ScopeAll_WithNonEmptyTargetIds_ShouldFail()
    {
        var dto = new CreateRoleDto(
            Name: "Scope All Violation",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("EmployeeManagement", new List<string> { "Add" }, new PermissionScopeDto("All", new List<int> { 1, 2 }))
            }
        );

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("TargetIds must be empty when ScopeType is All", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validators_ScopedPermission_WithNullOrEmptyTargetIds_ShouldFail()
    {
        var nullTargetsDto = new CreateRoleDto(
            Name: "Scope Department Violation 1",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("EmployeeManagement", new List<string> { "Add" }, new PermissionScopeDto("Department", null))
            }
        );

        var emptyTargetsDto = new CreateRoleDto(
            Name: "Scope Department Violation 2",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("EmployeeManagement", new List<string> { "Add" }, new PermissionScopeDto("Department", new List<int>()))
            }
        );

        var nullResult = _createValidator.Validate(nullTargetsDto);
        var emptyResult = _createValidator.Validate(emptyTargetsDto);

        Assert.False(nullResult.IsValid);
        Assert.False(emptyResult.IsValid);
    }

    [Fact]
    public void Validators_ScopedPermission_WithZeroOrNegativeTargetIds_ShouldFail()
    {
        var zeroTargetDto = new CreateRoleDto(
            Name: "Scope Site Violation 1",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("SiteManagement", new List<string> { "ManageShifts" }, new PermissionScopeDto("Site", new List<int> { 10, 0 }))
            }
        );

        var negativeTargetDto = new CreateRoleDto(
            Name: "Scope Site Violation 2",
            Description: null,
            Permissions: new List<ModulePermissionDto>
            {
                new ModulePermissionDto("SiteManagement", new List<string> { "ManageShifts" }, new PermissionScopeDto("Site", new List<int> { -5 }))
            }
        );

        var zeroResult = _createValidator.Validate(zeroTargetDto);
        var negativeResult = _createValidator.Validate(negativeTargetDto);

        Assert.False(zeroResult.IsValid);
        Assert.False(negativeResult.IsValid);
    }

    private static List<ModulePermissionDto> CreateValidPermissions()
    {
        return new List<ModulePermissionDto>
        {
            new ModulePermissionDto(
                Module: "EmployeeManagement",
                Actions: new List<string> { "Add", "Edit" },
                Scope: new PermissionScopeDto("All", null)
            )
        };
    }
}
