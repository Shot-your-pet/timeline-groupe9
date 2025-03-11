module ShotYourPet.Messaging.Model

open System
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization

type NewPublication = {
      [<JsonPropertyName("id")>]
      Id: int64
      [<JsonPropertyName("author_id")>]
      AuthorId: Guid
      [<JsonPropertyName("date")>]
      Date: DateTimeOffset
      [<JsonPropertyName("description")>]
      Description: string | null
      [<JsonPropertyName("image_id")>]
      ImageId: int64 }
and PublicationLiked = {
      [<JsonPropertyName("author_id")>]
      AuthorId: Guid
      [<JsonPropertyName("post_id")>]
      PostId: int64
}

type EventContent = | NewPublication of NewPublication | PublicationLiked of PublicationLiked

[<JsonConverter(typeof<EventConverter>)>]
type PublishingEvent = { Content : EventContent }
and EventConverter() =
      inherit JsonConverter<PublishingEvent>()
     
      override this.Read(reader, typeToConvert, options) =
            let item = JsonObject.Parse(&reader,  JsonNodeOptions()).AsObject()
            let content = item["content"]
            let content =
                  match item["type"].GetValue<string>() with
                  | "new_publication" -> EventContent.NewPublication(content.Deserialize<NewPublication>())
                  | "like" ->  EventContent.PublicationLiked(content.Deserialize<PublicationLiked>())
                  | s -> raise (JsonException( $"unknown item type %s{s}"))
            { Content = content }
            
      override this.Write(writer, value, options) = failwith "not implemented"
