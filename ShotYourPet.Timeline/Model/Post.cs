using System.ComponentModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace ShotYourPet.Timeline.Model;

public class Post
public record Post
{
    [JsonPropertyName("id")]
    [Description("Id of the post")]
    public required long Id { get; init; }

    [JsonPropertyName("author_id")]
    [Description("UUID of the post author")]
    public required Guid AuthorId { [UsedImplicitly] get; init; }

    [JsonPropertyName("challenge_id")]
    [Description("Id of the challenge associated with the post")]
    public required long ChallengeId { [UsedImplicitly] get; init; }

    [JsonPropertyName("published_at")]
    [Description("Publication date of the post")]
    public required DateTimeOffset PublishedAt { [UsedImplicitly] get; init; }

    [JsonPropertyName("content")]
    [Description("A small optional text attached to the post")]
    public required string? Content { [UsedImplicitly] get; init; }

    [JsonPropertyName("image_id")]
    [Description("Id of the post image")]
    public required long ImageId { [UsedImplicitly] get; init; }
}

public record CursoredPostList
{
    [JsonPropertyName("sizes")]
    [Description("Size of content")]
    public required int Size { [UsedImplicitly] get; init; }

    [JsonPropertyName("next_cursor")]
    [Description("Id of the next post list, or null if there is no element after content.")]
    public required long? NextCursor { [UsedImplicitly] get; init; }


    [JsonPropertyName("content")]
    [Description("List of posts")]
    public required List<Post> Content { [UsedImplicitly] get; init; }
}