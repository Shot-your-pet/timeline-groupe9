using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ShotYourPet.Timeline.Model;

public record ResponseApi<T>
{
    [JsonPropertyName("contenu")]
    [Description("The content of the response")]
    public required T Content { get; init; }

    [JsonPropertyName("code")]
    [Description("The http stastus code of the response")]
    public required int Code { get; init; }

    [JsonPropertyName("code")]
    [Description("The message for the response")]
    public required string? Message { get; init; }
}