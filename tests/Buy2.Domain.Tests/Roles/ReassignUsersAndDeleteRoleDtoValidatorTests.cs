using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.DeleteRole;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class ReassignUsersAndDeleteRoleDtoValidatorTests
{
    private readonly ReassignUsersAndDeleteRoleDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidDto_WithDefaultNewRoleId_ShouldPass()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: 2,
            Reassignments: null
        );

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidDto_WithReassignments_ShouldPass()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: null,
            Reassignments: new List<EmployeeReassignmentDto>
            {
                new EmployeeReassignmentDto(101, 2),
                new EmployeeReassignmentDto(102, 3)
            }
        );

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidDto_WithBothReassignmentsAndDefaultNewRoleId_ShouldPass()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: 3,
            Reassignments: new List<EmployeeReassignmentDto>
            {
                new EmployeeReassignmentDto(101, 2)
            }
        );

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingBothReassignmentsAndDefaultNewRoleId_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: null,
            Reassignments: null
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Must specify either a non-empty reassignments list or a valid default new role ID"));
    }

    [Fact]
    public void Validate_EmptyReassignmentsAndNullDefaultRoleId_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: null,
            Reassignments: new List<EmployeeReassignmentDto>()
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Must specify either a non-empty reassignments list or a valid default new role ID"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_DefaultNewRoleIdZeroOrNegative_ShouldFail(int invalidDefaultRoleId)
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: invalidDefaultRoleId
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("greater than 0"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReassignmentItem_InvalidEmployeeId_ShouldFail(int invalidEmployeeId)
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            Reassignments: new List<EmployeeReassignmentDto>
            {
                new EmployeeReassignmentDto(invalidEmployeeId, 2)
            }
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Employee ID must be greater than 0"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Validate_ReassignmentItem_InvalidNewRoleId_ShouldFail(int invalidNewRoleId)
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            Reassignments: new List<EmployeeReassignmentDto>
            {
                new EmployeeReassignmentDto(101, invalidNewRoleId)
            }
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("New role ID must be greater than 0"));
    }
}
