using System.Net;
using System.Net.Http.Json;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Contract tests for Admin API endpoints (US3: User Management)
/// T061: Validates that admin endpoints require ADMIN role and follow API contracts
/// </summary>
[Collection("Integration Tests")]
public class AdminContractTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminContractTests(CeibaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region User Management Endpoints - Authentication Required

    [Fact(DisplayName = "T061: GET /api/admin/users without authentication should return 401")]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: GET /api/admin/users/{id} without authentication should return 401")]
    public async Task GetUserById_WithoutAuth_Returns401()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/admin/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: POST /api/admin/users without authentication should return 401")]
    public async Task CreateUser_WithoutAuth_Returns401()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Nombre = "Test User",
            Email = "test@test.com",
            Password = "TestPassword123!",
            Roles = new List<string> { "CREADOR" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: PUT /api/admin/users/{id} without authentication should return 401")]
    public async Task UpdateUser_WithoutAuth_Returns401()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            Nombre = "Updated User",
            Email = "updated@test.com",
            Roles = new List<string> { "CREADOR" },
            Activo = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{userId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: POST /api/admin/users/{id}/suspend without authentication should return 401")]
    public async Task SuspendUser_WithoutAuth_Returns401()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/admin/users/{userId}/suspend", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: POST /api/admin/users/{id}/activate without authentication should return 401")]
    public async Task ActivateUser_WithoutAuth_Returns401()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/admin/users/{userId}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: DELETE /api/admin/users/{id} without authentication should return 401")]
    public async Task DeleteUser_WithoutAuth_Returns401()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/admin/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T061: GET /api/admin/roles without authentication should return 401")]
    public async Task GetRoles_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DTO Validation Tests

    [Fact(DisplayName = "T061: CreateUserDto should require all mandatory fields")]
    public void CreateUserDto_ShouldHaveRequiredFields()
    {
        // Arrange
        var dto = new CreateUserDto();

        // Act & Assert - Validation attributes exist on the DTO
        var nombreProperty = typeof(CreateUserDto).GetProperty(nameof(CreateUserDto.Nombre));
        var emailProperty = typeof(CreateUserDto).GetProperty(nameof(CreateUserDto.Email));
        var passwordProperty = typeof(CreateUserDto).GetProperty(nameof(CreateUserDto.Password));
        var rolesProperty = typeof(CreateUserDto).GetProperty(nameof(CreateUserDto.Roles));

        nombreProperty.Should().NotBeNull();
        emailProperty.Should().NotBeNull();
        passwordProperty.Should().NotBeNull();
        rolesProperty.Should().NotBeNull();

        // Verify Required attributes
        nombreProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().NotBeEmpty("Nombre should be required");
        emailProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().NotBeEmpty("Email should be required");
        passwordProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().NotBeEmpty("Password should be required");
        rolesProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().NotBeEmpty("Roles should be required");
    }

    [Fact(DisplayName = "T061: UpdateUserDto should require mandatory fields")]
    public void UpdateUserDto_ShouldHaveRequiredFields()
    {
        // Arrange & Act
        var nombreProperty = typeof(UpdateUserDto).GetProperty(nameof(UpdateUserDto.Nombre));
        var emailProperty = typeof(UpdateUserDto).GetProperty(nameof(UpdateUserDto.Email));
        var rolesProperty = typeof(UpdateUserDto).GetProperty(nameof(UpdateUserDto.Roles));
        var newPasswordProperty = typeof(UpdateUserDto).GetProperty(nameof(UpdateUserDto.NewPassword));

        // Assert
        nombreProperty.Should().NotBeNull();
        emailProperty.Should().NotBeNull();
        rolesProperty.Should().NotBeNull();
        newPasswordProperty.Should().NotBeNull();

        // NewPassword should be optional (no Required attribute)
        newPasswordProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().BeEmpty("NewPassword should be optional");
    }

    [Fact(DisplayName = "T061: UserDto should have all expected properties")]
    public void UserDto_ShouldHaveExpectedProperties()
    {
        // Arrange
        var dto = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Nombre = "Test User",
            Roles = new List<string> { "CREADOR" },
            Activo = true,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        // Assert
        dto.Id.Should().NotBeEmpty();
        dto.Email.Should().Be("test@test.com");
        dto.Nombre.Should().Be("Test User");
        dto.Roles.Should().Contain("CREADOR");
        dto.Activo.Should().BeTrue();
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dto.LastLogin.Should().NotBeNull();
    }

    [Fact(DisplayName = "T061: UserListResponse should support pagination")]
    public void UserListResponse_ShouldSupportPagination()
    {
        // Arrange
        var response = new UserListResponse
        {
            Items = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Nombre = "User 1", Email = "user1@test.com" }
            },
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };

        // Assert
        response.Items.Should().HaveCount(1);
        response.TotalCount.Should().Be(100);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
    }

    [Fact(DisplayName = "T061: UserFilterDto should have default pagination values")]
    public void UserFilterDto_ShouldHaveDefaultPaginationValues()
    {
        // Arrange
        var filter = new UserFilterDto();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(20);
        filter.Search.Should().BeNull();
        filter.Role.Should().BeNull();
        filter.Activo.Should().BeNull();
    }

    #endregion

    #region API Endpoint Routes

    [Fact(DisplayName = "T061: Admin API should expose correct endpoint routes")]
    public void AdminApi_ShouldExposeCorrectRoutes()
    {
        // This test validates the expected API routes are configured
        // The actual route configuration is validated by the controller attributes
        var controllerType = typeof(Ceiba.Web.Controllers.AdminController);

        // Verify route attribute
        var routeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true)
            .Cast<Microsoft.AspNetCore.Mvc.RouteAttribute>()
            .FirstOrDefault();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/[controller]");

        // Verify ApiController attribute
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), true)
            .Should().NotBeEmpty();
    }

    #endregion
}
