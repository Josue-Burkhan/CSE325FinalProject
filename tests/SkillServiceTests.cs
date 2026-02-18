using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Services;
using CSE325FinalProject.Data;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace CSE325FinalProject.Tests;

public class SkillServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateSkillAsync_ShouldCreateSkill_WithCorrectDefaults()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Categories.Add(new Category { Id = 1, Name = "Test Category", Color = "#000000" });
        context.Users.Add(new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@example.com", PasswordHash = "hash" });
        context.SaveChanges();
        
        var service = new SkillService(context);
        var userId = 1;
        
        var request = new CreateSkillRequest
        {
            Name = "Test Skill",
            Description = "A test skill",
            Visibility = "private",
            CategoryId = 1,
            TargetHours = 10
        };

        // Act
        var result = await service.CreateSkillAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Skill", result.Name);
        Assert.Equal("in_progress", result.Status);
        Assert.Equal(userId, result.UserId);
        Assert.Null(result.PublicSlug);
    }

    [Fact]
    public async Task CreateSkillAsync_ShouldGenerateSlug_WhenPublic()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Categories.Add(new Category { Id = 1, Name = "Test Category", Color = "#000000" });
        context.Users.Add(new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@example.com", PasswordHash = "hash" });
        context.SaveChanges();

        var service = new SkillService(context);
        var userId = 1;

        var request = new CreateSkillRequest
        {
            Name = "Public Skill",
            Visibility = "public",
            CategoryId = 1
        };

        // Act
        var result = await service.CreateSkillAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("public", result.Visibility);
        Assert.NotNull(service.GetSkillBySlugAsync(result.PublicSlug));
    }

    [Fact]
    public async Task UpdateSkillAsync_ShouldUpdateProperties_WhenSkillExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@example.com", PasswordHash = "hash" };
        var category = new Category { Id = 1, Name = "Test Category", Color = "#000000" };
        context.Users.Add(user);
        context.Categories.Add(category);
            
        var skill = new Skill 
        { 
            Id = 1, 
            UserId = 1, 
            CategoryId = 1, 
            Name = "Original Name", 
            Status = "in_progress",
            Visibility = "private",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Skills.Add(skill);
        context.SaveChanges();

        var service = new SkillService(context);
        var request = new UpdateSkillRequest { Name = "Updated Name", Status = "completed" };

        var result = await service.UpdateSkillAsync(1, 1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("completed", result.Status);
    }

    [Fact]
    public async Task DeleteSkillAsync_ShouldRemoveSkill_WhenAuthorized()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);
            
        var skill = new Skill 
        { 
            Id = 1, 
            UserId = 1, 
            Name = "To Delete", 
            Status = "in_progress",
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };
        context.Skills.Add(skill);
        context.SaveChanges();

        var service = new SkillService(context);

        // Act
        var result = await service.DeleteSkillAsync(1, 1);
        var checkedSkill = await context.Skills.FindAsync(1);

        // Assert
        Assert.True(result);
        Assert.Null(checkedSkill);
    }
}
