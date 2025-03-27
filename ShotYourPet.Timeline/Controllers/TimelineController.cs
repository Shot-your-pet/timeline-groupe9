using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShotYourPet.Database;
using ShotYourPet.Timeline.Model;
using Author = ShotYourPet.Timeline.Model.Author;
using Post = ShotYourPet.Timeline.Model.Post;

namespace ShotYourPet.Timeline.Controllers;

[ApiController]
[Route("/timeline")]
public class TimelineController(ILogger<TimelineController> logger, TimelineDbContext context) : ControllerBase
{
    private readonly ILogger<TimelineController> _logger = logger;

    [HttpGet(Name = "Default timeline")]
    [EndpointSummary("Get default timeline")]
    [EndpointDescription("""
                         Get the default timeline.
                         This is the timeline with all posts, sorted by date descending.
                         """)]
    public async Task<ResponseApi<CursoredPostList>> Get(
        [Description("Return posts posted before this id")]
        long? cursor = null,
        [Description("Number of elements to return, limited to 25")]
        int limit = 25)
    {
        var query = from p in context.Posts
            orderby p.Id descending
            where cursor == null || p.Id <= cursor
            select new Post
            {
                Id = p.Id,
                Author = new Author
                {
                    Id = p.Author.Id,
                    Pseudo = p.Author.Pseudo,
                    AvatarId = p.Author.AvatarId
                },
                ChallengeId = p.ChallengeId,
                PublishedAt = p.PublishedAt,
                Content = p.Content,
                ImageId = p.ImageId
            };

        var size = Math.Clamp(limit, 0, 25) + 1;
        var list = await query.Take(size).ToListAsync();

        var totalSize = context.Posts.Count();

        var res = new CursoredPostList
        {
            Size = size == list.Count ? list.Count - 1 : list.Count,
            TotalSize = totalSize,
            NextCursor = size == list.Count ? list.Skip(size - 1).FirstOrDefault()?.Id : null,
            Content = list.Count == size ? list[..^1] : list
        };

        return new ResponseApi<CursoredPostList>()
        {
            Content = res,
            Code = 200,
            Message = null
        };
    }

    [HttpGet("{authorId:guid}", Name = "Author timeline")]
    [EndpointSummary("Get author timeline")]
    [EndpointDescription("""
                         Get post list of an author.
                         This is the timeline with all posts of the author, sorted by date descending.
                         """)]
    public async Task<ResponseApi<CursoredPostList>> GetForAuthor(
        [Description("Id of the author")] Guid authorId,
        [Description("Return posts posted before this id")]
        long? cursor = null,
        [Description("Number of elements to return, limited to 25")]
        int limit = 25)
    {
        var query = from p in context.Posts
            where p.Author.Id == authorId
            orderby p.Id descending
            where cursor == null || p.Id <= cursor
            select new Post
            {
                Id = p.Id,
                Author = new Author
                {
                    Id = p.Author.Id,
                    Pseudo = p.Author.Pseudo,
                    AvatarId = p.Author.AvatarId
                },
                ChallengeId = p.ChallengeId,
                PublishedAt = p.PublishedAt,
                Content = p.Content,
                ImageId = p.ImageId
            };

        var size = Math.Clamp(limit, 0, 25) + 1;
        var list = await query.Take(size).ToListAsync();

        var totalSize = (from p in context.Posts where p.Author.Id == authorId select p).Count();

        var res = new CursoredPostList
        {
            Size = size == list.Count ? list.Count - 1 : list.Count,
            TotalSize = totalSize,
            NextCursor = size == list.Count ? list.Skip(size - 1).FirstOrDefault()?.Id : null,
            Content = list.Count == size ? list[..^1] : list
        };

        return new ResponseApi<CursoredPostList>()
        {
            Content = res,
            Code = 200,
            Message = null
        };
    }
}