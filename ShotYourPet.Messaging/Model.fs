module ShotYourPet.Messaging.Model

open System
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization

type NewPublication =
    { [<JsonPropertyName("id")>]
      Id: int64
      [<JsonPropertyName("author_id")>]
      AuthorId: Guid
      [<JsonPropertyName("challenge_id")>]
      ChallengeId: int64
      [<JsonPropertyName("date")>]
      Date: DateTimeOffset
      [<JsonPropertyName("content")>]
      Content: string | null
      [<JsonPropertyName("image_id")>]
      ImageId: int64 }

and PublicationLiked =
    { [<JsonPropertyName("author_id")>]
      AuthorId: Guid
      [<JsonPropertyName("post_id")>]
      PostId: int64 }

type EventContent =
    | NewPublication of NewPublication
    | PublicationLiked of PublicationLiked

[<JsonConverter(typeof<EventConverter>)>]
type PublishingEvent = { Content: EventContent }

and EventConverter() =
    inherit JsonConverter<PublishingEvent | null>()

    override this.CanConvert(typeToConvert) =
        typeToConvert = typeof<PublishingEvent | null>

    override this.Read(reader, _, options) : PublishingEvent | null =
        match
            JsonObject.Parse(&reader, JsonNodeOptions())
            |> Option.ofObj
            |> Option.map _.AsObject()
        with
        | Some item ->
            let contentObj = item["content"] |> Option.ofObj
            let item = item["type"] |> Option.ofObj |> Option.map _.GetValue<string>()

            let content =
                match item with
                | Some "new_publication" ->
                    let newPublication =
                        contentObj
                        |> Option.map (fun x -> x.Deserialize<NewPublication>() |> Option.ofObj)
                        |> Option.flatten

                    match newPublication with
                    | Some x -> EventContent.NewPublication(x)
                    | None -> raise (JsonException("Missing content"))
                | Some "like" ->
                    let likeContent =
                        contentObj
                        |> Option.map (fun x -> x.Deserialize<PublicationLiked>() |> Option.ofObj)
                        |> Option.flatten

                    match likeContent with
                    | Some x -> EventContent.PublicationLiked(x)
                    | None -> raise (JsonException("Missing content"))
                | Some t -> raise (JsonException($"Unknown item type %s{t}"))
                | None -> raise (JsonException("Missing item type"))

            { Content = content }
        | None -> null

    override this.Write(_, _, _) = failwith "not implemented"
