using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Data;
using EduHub.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EduHub.Tests;

public class UnitOfWorkAndRepositoryTests
{
    private EduHubDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<EduHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EduHubDbContext(options);
    }

    [Fact]
    public async Task UnitOfWork_SavesEntityAndQueriesViaRepository()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        using var uow = new UnitOfWork(context);

        var category = new Category
        {
            Name = "Cloud Computing",
            Slug = "cloud",
            IconClass = "bi-cloud"
        };

        // Act
        await uow.Categories.AddAsync(category);
        await uow.SaveChangesAsync();

        var retrieved = await uow.Categories.SingleOrDefaultAsync(c => c.Slug == "cloud");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Cloud Computing", retrieved.Name);
    }

    [Fact]
    public async Task CourseRepository_FiltersPublishedCoursesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        using var uow = new UnitOfWork(context);

        var instructor = new ApplicationUser { Id = "inst1", UserName = "inst1@test.com", FullName = "Instructor One" };
        await context.Users.AddAsync(instructor);

        var cat = new Category { Name = "Web", Slug = "web" };
        await uow.Categories.AddAsync(cat);
        await uow.SaveChangesAsync();

        var coursePublished = new Course
        {
            Title = "Published C# Course",
            Description = "Learn C#",
            Status = CourseStatus.Published,
            Price = 50m,
            CategoryId = cat.Id,
            InstructorId = instructor.Id,
            CreatedAt = DateTime.UtcNow
        };

        var courseDraft = new Course
        {
            Title = "Draft Course",
            Description = "Draft",
            Status = CourseStatus.Draft,
            Price = 20m,
            CategoryId = cat.Id,
            InstructorId = instructor.Id,
            CreatedAt = DateTime.UtcNow
        };

        await uow.Courses.AddAsync(coursePublished);
        await uow.Courses.AddAsync(courseDraft);
        await uow.SaveChangesAsync();

        // Act
        var publishedList = await uow.Courses.GetPublishedCoursesAsync();

        // Assert
        Assert.Single(publishedList);
        Assert.Equal("Published C# Course", publishedList.First().Title);
    }
}
