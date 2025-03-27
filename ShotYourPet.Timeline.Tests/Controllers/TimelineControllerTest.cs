using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShotYourPet.Database;
using ShotYourPet.Timeline.Controllers;
using Xunit;
using Xunit.Abstractions;

namespace ShotYourPet.Timeline.Tests.Controllers;

[TestSubject(typeof(TimelineController))]
public class TimelineControllerTest
{
    private readonly TimelineDbContext _dbContext;
    private readonly TimelineController _controller;

    public TimelineControllerTest(ITestOutputHelper helper)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TimelineDbContext>()
            .UseSqlite(connection,
                s => { s.MigrationsHistoryTable("__MigrationHistory"); }).Options;
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => { builder.AddDebug(); })
            .BuildServiceProvider();
        _dbContext = new TimelineDbContext(options);

        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        var logger = serviceProvider.GetService<ILogger<TimelineController>>();
        _controller = new TimelineController(logger, _dbContext);
    }

    [Fact]
    public async Task Get_WithEmptyDb_ReturnOk()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        var result = await _controller.Get();
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(0, result.Content.TotalSize);
        Assert.Equal(0, result.Content.Size);
        Assert.Equal([], result.Content.Content);
        Assert.Null(result.Content.NextCursor);
    }

    [Fact]
    public async Task Get_WithNoCursor_ReturnOk()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        var authorId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var publishedAt = DateTime.UtcNow;
        var imageId = 10;

        var author = new Author
        {
            Id = authorId,
            Pseudo = "foo"
        };


        _dbContext.Authors.Add(author);
        for (var i = 0; i < 25; i++)
            _dbContext.Posts.Add(new Post
            {
                Author = author,
                Content = $"foo {i}",
                ChallengeId = challengeId,
                PublishedAt = publishedAt,
                ImageId = imageId,
                Id = i
            });

        await _dbContext.SaveChangesAsync();


        var result = await _controller.Get();
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(25, result.Content.TotalSize);
        Assert.Equal(25, result.Content.Size);
        Assert.Null(result.Content.NextCursor);
        Assert.Equal(Enumerable.Range(0, 25).OrderDescending().Select(s => new Model.Post
        {
            Author = new Model.Author()
            {
                Id = authorId,
                Pseudo = "foo",
                AvatarId = null
            },
            Content = $"foo {s}",
            ChallengeId = challengeId,
            PublishedAt = publishedAt,
            ImageId = imageId,
            Id = s
        }).ToList(), result.Content.Content);
    }

    [Fact]
    public async Task Get_FullCursor_ReturnOk()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        var authorId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var publishedAt = DateTime.UtcNow;
        var imageId = 10;

        var author = new Author
        {
            Id = authorId,
            Pseudo = "foo"
        };


        _dbContext.Authors.Add(author);
        for (var i = 0; i < 50; i++)
            _dbContext.Posts.Add(new Post
            {
                Author = author,
                Content = $"foo {i}",
                ChallengeId = challengeId,
                PublishedAt = publishedAt,
                ImageId = imageId,
                Id = i
            });

        await _dbContext.SaveChangesAsync();


        var result = await _controller.Get();
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(50, result.Content.TotalSize);
        Assert.Equal(25, result.Content.Size);
        Assert.Equal(24, result.Content.NextCursor);
        Assert.Equal(Enumerable.Range(25, 25).OrderDescending().Select(s => new Model.Post
        {
            Author = new Model.Author()
            {
                Id = authorId,
                Pseudo = "foo",
                AvatarId = null
            },
            Content = $"foo {s}",
            ChallengeId = challengeId,
            PublishedAt = publishedAt,
            ImageId = imageId,
            Id = s
        }).ToList(), result.Content.Content);


        result = await _controller.Get(result.Content.NextCursor);
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(50, result.Content.TotalSize);
        Assert.Equal(25, result.Content.Size);
        Assert.Null(result.Content.NextCursor);
        Assert.Equal(Enumerable.Range(0, 25).OrderDescending().Select(s => new Model.Post
        {
            Author = new Model.Author()
            {
                Id = authorId,
                Pseudo = "foo",
                AvatarId = null
            },
            Content = $"foo {s}",
            ChallengeId = challengeId,
            PublishedAt = publishedAt,
            ImageId = imageId,
            Id = s
        }).ToList(), result.Content.Content);
    }

    // Get for author

    [Fact]
    public async Task GetForAuthor_WithEmptyDb_ReturnOk()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        var authorId = Guid.NewGuid();

        var result = await _controller.GetForAuthor(authorId);
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(0, result.Content.TotalSize);
        Assert.Equal(0, result.Content.Size);
        Assert.Equal([], result.Content.Content);
        Assert.Null(result.Content.NextCursor);
    }

    [Fact]
    public async Task GetForAuthor_WithNoCursor_ReturnOk()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        var authorId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var publishedAt = DateTime.UtcNow;
        var imageId = 10;

        var author = new Author
        {
            Id = authorId,
            Pseudo = "foo"
        };


        var otherAuthor = new Author
        {
            Id = Guid.NewGuid(),
            Pseudo = "bar"
        };

        _dbContext.Authors.AddRange(author, otherAuthor);
        for (var i = 0; i < 25; i++)
            _dbContext.Posts.Add(new Post
            {
                Author = author,
                Content = $"foo {i}",
                ChallengeId = challengeId,
                PublishedAt = publishedAt,
                ImageId = imageId,
                Id = i
            });


        for (var i = 0; i < 25; i++)
            _dbContext.Posts.Add(new Post
            {
                Author = otherAuthor,
                Content = $"bar {i}",
                ChallengeId = challengeId,
                PublishedAt = publishedAt,
                ImageId = imageId,
                Id = 5000 + i
            });

        await _dbContext.SaveChangesAsync();


        var result = await _controller.GetForAuthor(authorId);
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(25, result.Content.TotalSize);
        Assert.Equal(25, result.Content.Size);
        Assert.Null(result.Content.NextCursor);
        Assert.Equal(Enumerable.Range(0, 25).OrderDescending().Select(s => new Model.Post
        {
            Author = new Model.Author()
            {
                Id = authorId,
                Pseudo = "foo",
                AvatarId = null
            },
            Content = $"foo {s}",
            ChallengeId = challengeId,
            PublishedAt = publishedAt,
            ImageId = imageId,
            Id = s
        }).ToList(), result.Content.Content);
    }

    [Fact]
    public async Task GetForAuthor_FullCursor_ReturnOk()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        var authorId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var publishedAt = DateTime.UtcNow;
        var imageId = 10;

        var author = new Author
        {
            Id = authorId,
            Pseudo = "foo"
        };


        var otherAuthor = new Author
        {
            Id = Guid.NewGuid(),
            Pseudo = "bar"
        };

        _dbContext.Authors.AddRange(author, otherAuthor);
        for (var i = 0; i < 50; i++)
            _dbContext.Posts.Add(new Post
            {
                Author = author,
                Content = $"foo {i}",
                ChallengeId = challengeId,
                PublishedAt = publishedAt,
                ImageId = imageId,
                Id = i
            });

        for (var i = 0; i < 25; i++)
            _dbContext.Posts.Add(new Post
            {
                Author = otherAuthor,
                Content = $"bar {i}",
                ChallengeId = challengeId,
                PublishedAt = publishedAt,
                ImageId = imageId,
                Id = 5000 + i
            });

        await _dbContext.SaveChangesAsync();


        var result = await _controller.GetForAuthor(authorId);
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(50, result.Content.TotalSize);
        Assert.Equal(25, result.Content.Size);
        Assert.Equal(24, result.Content.NextCursor);
        Assert.Equal(Enumerable.Range(25, 25).OrderDescending().Select(s => new Model.Post
        {
            Author = new Model.Author()
            {
                Id = authorId,
                Pseudo = "foo",
                AvatarId = null
            },
            Content = $"foo {s}",
            ChallengeId = challengeId,
            PublishedAt = publishedAt,
            ImageId = imageId,
            Id = s
        }).ToList(), result.Content.Content);


        result = await _controller.GetForAuthor(authorId, result.Content.NextCursor);
        Assert.Equal(200, result.Code);
        Assert.Null(result.Message);
        Assert.Equal(50, result.Content.TotalSize);
        Assert.Equal(25, result.Content.Size);
        Assert.Null(result.Content.NextCursor);
        Assert.Equal(Enumerable.Range(0, 25).OrderDescending().Select(s => new Model.Post
        {
            Author = new Model.Author()
            {
                Id = authorId,
                Pseudo = "foo",
                AvatarId = null
            },
            Content = $"foo {s}",
            ChallengeId = challengeId,
            PublishedAt = publishedAt,
            ImageId = imageId,
            Id = s
        }).ToList(), result.Content.Content);
    }
}