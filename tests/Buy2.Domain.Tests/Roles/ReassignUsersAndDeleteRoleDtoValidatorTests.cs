using Buy2.Application.DTOs.Roles;
using Buy2.Application.Validators;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class ReassignUsersAndDeleteRoleDtoValidatorTests
{
    private readonly ReassignUsersAndDeleteRoleDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidDto_WithDefaultNewRoleId_ShouldPass()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: null,
            DefaultNewRoleId: 2
        );

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidDto_WithReassignments_ShouldPass()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>
            {
                new UserRoleReassignmentItemDto(101, 2),
                new UserRoleReassignmentItemDto(102, 3)
            },
            DefaultNewRoleId: null
        );

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidDto_WithBothReassignmentsAndDefaultNewRoleId_ShouldPass()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>
            {
                new UserRoleReassignmentItemDto(101, 2)
            },
            DefaultNewRoleId: 3
        );

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidRoleId_ShouldFail(int invalidRoleId)
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: invalidRoleId,
            DefaultNewRoleId: 2
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReassignUsersAndDeleteRoleDto.RoleId) && e.ErrorMessage.Contains("greater than 0"));
    }

    [Fact]
    public void Validate_MissingBothReassignmentsAndDefaultNewRoleId_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: null,
            DefaultNewRoleId: null
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Must specify either a non-empty reassignments list or a valid default new role ID"));
    }

    [Fact]
    public void Validate_EmptyReassignmentsAndNullDefaultRoleId_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>(),
            DefaultNewRoleId: null
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
            RoleId: 10,
            DefaultNewRoleId: invalidDefaultRoleId
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("greater than 0"));
    }

    [Fact]
    public void Validate_DefaultNewRoleIdSameAsRoleId_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            DefaultNewRoleId: 10
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Default replacement role cannot be the same role being deleted."));
    }

    [Fact]
    public void Validate_DuplicateEmployeeIdsInReassignments_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>
            {
                new UserRoleReassignmentItemDto(101, 2),
                new UserRoleReassignmentItemDto(101, 3)
            }
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("duplicate employee entries"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReassignmentItem_InvalidEmployeeId_ShouldFail(int invalidEmployeeId)
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>
            {
                new UserRoleReassignmentItemDto(invalidEmployeeId, 2)
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
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>
            {
                new UserRoleReassignmentItemDto(101, invalidNewRoleId)
            }
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("New role ID must be greater than 0"));
    }

    [Fact]
    public void Validate_ReassignmentItem_NewRoleIdSameAsRoleId_ShouldFail()
    {
        var dto = new ReassignUsersAndDeleteRoleDto(
            RoleId: 10,
            Reassignments: new List<UserRoleReassignmentItemDto>
            {
                new UserRoleReassignmentItemDto(101, 10)
            }
        );

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Replacement role cannot be the same role being deleted."));
    }
}
