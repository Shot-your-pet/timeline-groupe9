using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShotYourPet.Database;
using ShotYourPet.Timeline.Model;
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
    public async Task<CursoredPostList> Get(
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
                AuthorId = p.Author.Id,
                ChallengeId = p.ChallengeId,
                PublishedAt = p.PublishedAt,
                Content = p.Content,
                ImageId = p.ImageId
            };

        var size = Math.Clamp(limit, 0, 25) + 1;
        var list = await query.Take(size).ToListAsync();
        return new CursoredPostList
        {
            Size = Math.Max(0, list.Count - 1),
            NextCursor = size == list.Count ? list.Skip(size - 1).FirstOrDefault()?.Id : null,
            Content = list.Count == size ? list[..^1] : list
        };
    }
}