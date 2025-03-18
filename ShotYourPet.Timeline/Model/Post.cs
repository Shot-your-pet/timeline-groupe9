using System.ComponentModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace ShotYourPet.Timeline.Model;

public record Author
{
    [JsonPropertyName("id")]
    [Description("Id of the author")]
    public required Guid Id { get; init; }

    [JsonPropertyName("pseudo")]
    [Description("Nickname of the author")]
    public required string Pseudo { get; init; }

    [JsonPropertyName("avatar_id")]
    [Description("Id of the avatar of the author")]
    public required long? AvatarId { get; init; }
}

public record Post
{
    [JsonPropertyName("id")]
    [Description("Id of the post")]
    public required long Id { get; init; }

    [JsonPropertyName("author")]
    [Description("The post author")]
    public required Author Author { [UsedImplicitly] get; init; }

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
    [JsonPropertyName("size")]
    [Description("Size of content")]
    public required long Size { [UsedImplicitly] get; init; }

    [JsonPropertyName("total_size")]
    [Description("Total number of posts")]
    public required long TotalSize { get; init; }

    [JsonPropertyName("next_cursor")]
    [Description("Id of the next post list, or null if there is no element after content.")]
    public required long? NextCursor { [UsedImplicitly] get; init; }

    [JsonPropertyName("content")]
    [Description("List of posts")]
    public required List<Post> Content { [UsedImplicitly] get; init; }
}