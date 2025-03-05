using Microsoft.AspNetCore.Mvc;
using ShotYourPet.Database;
using Post = ShotYourPet.Timeline.Model.Post;

namespace ShotYourPet.Timeline.Controllers;

[ApiController]
[Route("[controller]")]
public class TimelineController(ILogger<TimelineController> logger, TimelineDbContext context) : ControllerBase
{
    private readonly ILogger<TimelineController> _logger = logger;

    /// <summary>
    ///     List all users
    /// </summary>
    /// <param name="cursor">String to search for in user IDs and names</param>
    /// <returns>An array of users</returns>
    /// <response code="200">OK</response>
    [HttpGet(Name = "GetTimeline")]
    public IEnumerable<Post> Get(long? cursor = null)
    {
        return from p in context.Posts
            orderby p.Id descending
            where cursor == null || p.Id < cursor
            select new Post
            {
                Id = p.Id,
                AuthorId = p.Author.Id
            };
    }
}