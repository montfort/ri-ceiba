using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for BaseEntity, BaseEntityWithUser, and BaseCatalogEntity classes.
/// Tests inheritance hierarchy and default values.
/// </summary>
[Trait("Category", "Unit")]
public class BaseEntityTests
{
    #region BaseEntity Tests

    [Fact(DisplayName = "BaseEntity derived class should have CreatedAt set to UTC now by default")]
    public void BaseEntity_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var entity = new TestBaseEntity();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        entity.CreatedAt.Should().BeAfter(before);
        entity.CreatedAt.Should().BeBefore(after);
        entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact(DisplayName = "BaseEntity CreatedAt should be settable")]
    public void BaseEntity_CreatedAt_ShouldBeSettable()
    {
        // Arrange
        var entity = new TestBaseEntity();
        var specificDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        entity.CreatedAt = specificDate;

        // Assert
        entity.CreatedAt.Should().Be(specificDate);
    }

    #endregion

    #region BaseEntityWithUser Tests

    [Fact(DisplayName = "BaseEntityWithUser should have default Guid for UsuarioId")]
    public void BaseEntityWithUser_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var entity = new TestBaseEntityWithUser();

        // Assert
        entity.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "BaseEntityWithUser UsuarioId should be settable")]
    public void BaseEntityWithUser_UsuarioId_ShouldBeSettable()
    {
        // Arrange
        var entity = new TestBaseEntityWithUser();
        var userId = Guid.NewGuid();

        // Act
        entity.UsuarioId = userId;

        // Assert
        entity.UsuarioId.Should().Be(userId);
    }

    [Fact(DisplayName = "BaseEntityWithUser should inherit CreatedAt from BaseEntity")]
    public void BaseEntityWithUser_ShouldInheritCreatedAt()
    {
        // Arrange & Act
        var entity = new TestBaseEntityWithUser();

        // Assert
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region BaseCatalogEntity Tests

    [Fact(DisplayName = "BaseCatalogEntity Activo should default to true")]
    public void BaseCatalogEntity_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var entity = new TestBaseCatalogEntity();

        // Assert
        entity.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "BaseCatalogEntity Activo should be settable to false")]
    public void BaseCatalogEntity_Activo_ShouldBeSettableToFalse()
    {
        // Arrange
        var entity = new TestBaseCatalogEntity();

        // Act
        entity.Activo = false;

        // Assert
        entity.Activo.Should().BeFalse();
    }

    [Fact(DisplayName = "BaseCatalogEntity should inherit UsuarioId from BaseEntityWithUser")]
    public void BaseCatalogEntity_ShouldInheritUsuarioId()
    {
        // Arrange
        var entity = new TestBaseCatalogEntity();
        var userId = Guid.NewGuid();

        // Act
        entity.UsuarioId = userId;

        // Assert
        entity.UsuarioId.Should().Be(userId);
    }

    [Fact(DisplayName = "BaseCatalogEntity should inherit CreatedAt from BaseEntity")]
    public void BaseCatalogEntity_ShouldInheritCreatedAt()
    {
        // Arrange & Act
        var entity = new TestBaseCatalogEntity();

        // Assert
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Test Helper Classes

    private class TestBaseEntity : BaseEntity { }

    private class TestBaseEntityWithUser : BaseEntityWithUser { }

    private class TestBaseCatalogEntity : BaseCatalogEntity { }

    #endregion
}
