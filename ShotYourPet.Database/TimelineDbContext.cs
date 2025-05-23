using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShotYourPet.Database;

public class TimelineDbContext(DbContextOptions<TimelineDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors { get; set; }
    public DbSet<Post> Posts { get; set; }
}

public class Author
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; init; }

    public required string Pseudo { get; init; } = null!;

    public long? AvatarId { get; init; }

    public ICollection<Post> Posts { get; init; } = null!;
}

public class Post
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; init; }

    [Required] public Author Author { get; init; } = null!;

    public Guid ChallengeId { get; init; }

    public string? Content { get; init; }

    public DateTimeOffset PublishedAt { get; init; }

    public long ImageId { get; init; }
}