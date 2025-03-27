using System.ComponentModel;
using System.Text.Json.Serialization;

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