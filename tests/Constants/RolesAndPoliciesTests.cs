using ApiGateway.Constants;
using FluentAssertions;
using Xunit;

namespace Gateway.Tests.Constants;

public class RolesTests
{
    [Theory]
    [InlineData("ADMIN")]
    [InlineData("CUSTOMER")]
    [InlineData("KITCHEN")]
    [InlineData("OPERATOR")]
    [InlineData("MANAGER")]
    public void AllRoles_ShouldHaveCorrectValues(string expectedRole)
    {
        // Act & Assert
        typeof(Roles).GetField(expectedRole)?.GetValue(null)
            .Should().Be(expectedRole);
    }

    [Fact]
    public void Roles_ShouldHaveExactlyFiveConstants()
    {
        // Arrange & Act
        var roleFields = typeof(Roles).GetFields();

        // Assert
        roleFields.Should().HaveCount(5);
    }
}

public class PoliciesTests
{
    [Theory]
    [InlineData("ADMIN_ONLY", "AdminOnly")]
    [InlineData("ADMIN_OR_MANAGER", "AdminOrManager")]
    [InlineData("KITCHEN_STAFF", "KitchenStaff")]
    [InlineData("OPERATORS", "Operators")]
    [InlineData("MANAGEMENT", "Management")]
    [InlineData("AUTHENTICATED_USER", "AuthenticatedUser")]
    [InlineData("ORDER_MANAGEMENT", "OrderManagement")]
    [InlineData("DATA_QUERY", "DataQuery")]
    public void AllPolicies_ShouldHaveCorrectValues(string fieldName, string expectedValue)
    {
        // Act & Assert
        typeof(Policies).GetField(fieldName)?.GetValue(null)
            .Should().Be(expectedValue);
    }

    [Fact]
    public void Policies_ShouldHaveExactlyEightConstants()
    {
        // Arrange & Act
        var policyFields = typeof(Policies).GetFields();

        // Assert
        policyFields.Should().HaveCount(8);
    }
}