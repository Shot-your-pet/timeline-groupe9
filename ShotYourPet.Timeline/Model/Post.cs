namespace ShotYourPet.Timeline.Model;

public class Post
{
    public required long Id { get; init; }

    public required Guid AuthorId { get; init; }

    public required long ChallengeId { get; init; }

    public required DateTimeOffset PublishedAt { get; init; }

    public required string? Content { get; init; }

    public required long ImageId { get; init; }
}

public class CursoredPostList
{
    public required int Size { get; init; }

    public required long? NextCursor { get; init; }

    public required List<Post> Content { get; init; }
}