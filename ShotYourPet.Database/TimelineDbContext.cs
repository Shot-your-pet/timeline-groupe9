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

    public ICollection<Post> Posts { get; init; } = null!;
}

public class Post
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; init; }

    [Required] public Author Author { get; init; } = null!;
}