using System.ComponentModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace ShotYourPet.Timeline.Model;

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
    public required Guid ChallengeId { [UsedImplicitly] get; init; }

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