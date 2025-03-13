module ShotYourPet.Messaging.Tests

open JetBrains.Annotations
open ShotYourPet.Messaging.Model
open Xunit
open System
open System.Text.Json

[<TestSubject(typeof<EventContent>)>]
type PublishingEventTests() =

    [<Fact>]
    member this.``Deserialize NewPublication Event``() =
        // Arrange
        let json =
            """
        {
            "content": {
                "id": 1,
                "author_id": "6eb6c444-fdf8-415d-b815-fb89469ad214",
                "challenge_id": 42,
                "date": "2023-10-01T12:00:00Z",
                "content": "A new publication",
                "image_id": 123
            },
            "type": "new_publication"
        }"""

        // Act
        let event = JsonSerializer.Deserialize<PublishingEvent>(json)

        // Assert
        match event.Content with
        | EventContent.NewPublication pub ->
            Assert.Equal(1L, pub.Id)
            Assert.Equal(Guid.Parse("6eb6c444-fdf8-415d-b815-fb89469ad214"), pub.AuthorId)
            Assert.Equal(42L, pub.ChallengeId)
            Assert.Equal(DateTimeOffset.Parse("2023-10-01T12:00:00Z"), pub.Date)
            Assert.Equal("A new publication", pub.Content)
            Assert.Equal(123L, pub.ImageId)
        | _ -> Assert.True(false, "Expected NewPublication event")

    [<Fact>]
    member this.``Deserialize NewPublication Event no content``() =
        // Arrange
        let json =
            """
        {
            "type": "new_publication",
            "content": null
        }"""

        let ex =
            Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<PublishingEvent>(json) |> ignore)

        Assert.Contains("Missing content", ex.Message)

    [<Fact>]
    member this.``Deserialize PublicationLiked Event``() =
        // Arrange
        let json =
            """
        {
            "type": "like",
            "content": {
                "author_id": "6eb6c444-fdf8-415d-b815-fb89469ad214",
                "post_id": 1
            }
        }"""

        // Act
        let event = JsonSerializer.Deserialize<PublishingEvent>(json)

        // Assert
        match event.Content with
        | EventContent.PublicationLiked like ->
            Assert.Equal(Guid.Parse("6eb6c444-fdf8-415d-b815-fb89469ad214"), like.AuthorId)
            Assert.Equal(1L, like.PostId)
        | _ -> Assert.True(false, "Expected PublicationLiked event")

    [<Fact>]
    member this.``Deserialize PublicationLiked Event no content``() =
        // Arrange
        let json =
            """
        {
            "type": "like",
            "content": null
        }"""

        let ex =
            Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<PublishingEvent>(json) |> ignore)

        Assert.Contains("Missing content", ex.Message)

    [<Fact>]
    member this.``Deserialize Unknown Event Type``() =
        // Arrange
        let json =
            """
        {
            "type": "unknown_event",
            "content": {}
        }"""

        // Act & Assert
        let ex =
            Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<PublishingEvent>(json) |> ignore)

        Assert.Contains("Unknown item type unknown_event", ex.Message)

    [<Fact>]
    member this.``Deserialize Missing Event Type``() =
        // Arrange
        let json =
            """
        {
            "content": {}
        }"""

        // Act & Assert
        let ex =
            Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<PublishingEvent>(json) |> ignore)

        Assert.Contains("Missing item type", ex.Message)

// Additional tests can be added for edge cases, null values, etc.
