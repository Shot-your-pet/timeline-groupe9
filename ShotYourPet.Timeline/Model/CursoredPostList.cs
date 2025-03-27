using System.ComponentModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace ShotYourPet.Timeline.Model;

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