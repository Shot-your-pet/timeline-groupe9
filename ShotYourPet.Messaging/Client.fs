namespace ShotYourPet.Messaging

open System
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open RabbitMQ.Client
open ShotYourPet.Database
open ShotYourPet.Messaging.Model
open ShotYourPet.Messaging.DbUtils

module private Client =
    type ParsingConsumer(channel: IChannel, logger: ILogger, timelineDbContext: TimelineDbContext) =
        inherit AsyncDefaultBasicConsumer(channel)

        override this.HandleBasicDeliverAsync(_, _, _, _, _, _, body, cancellationToken) =
            task {
                try
                    match
                        JsonSerializer.Deserialize<PublishingEvent>(body.Span)
                        |> Option.ofObj
                        |> Option.map _.Content
                    with
                    | Some(EventContent.NewPublication(e)) ->
                        do!
                            timelineDbContext.AddPost(
                                e.Id,
                                e.AuthorId,
                                e.ChallengeId,
                                e.Content,
                                e.Date,
                                e.ImageId,
                                cancellationToken
                            )
                    | Some value -> failwithf $"Not implemented : %A{value}"
                    | None -> ()
                with e ->
                    logger.LogError(e, "Failed to parse message")
            }

    type MessageScopedService(connection: IConnection, logger: ILogger) =
        member this.StopAsync token =
            task { do! connection.CloseAsync(cancellationToken = token) }

        interface IAsyncDisposable with
            member this.DisposeAsync() =
                task {
                    logger.LogWarning "Dispose"
                    do! connection.DisposeAsync()
                }
                |> ValueTask

    let getPublicationQueue
        (rabbitMqConfiguration: IConfiguration)
        (channel: IChannel)
        (stoppingToken: CancellationToken)
        =
        task {
            let exchangeName =
                rabbitMqConfiguration["TimelineExchangeName"]
                |> Option.ofObj
                |> Option.defaultValue ""

            let queueName =
                rabbitMqConfiguration["TimelineQueueName"]
                |> Option.ofObj
                |> Option.defaultValue "timeline.publish_posts"

            let routingKey =
                rabbitMqConfiguration["TimelineRoutingKey"]
                |> Option.ofObj
                |> Option.defaultValue "publication.publication_events"


            let! queue = channel.QueueDeclareAsync(queueName, true, false, false, cancellationToken = stoppingToken)

            // No need to bind if exchange is default
            if exchangeName <> "" then
                do!
                    channel.ExchangeDeclareAsync(
                        exchangeName,
                        ExchangeType.Direct,
                        durable = true,
                        autoDelete = false,
                        cancellationToken = stoppingToken
                    )

                do! channel.QueueBindAsync(queue.QueueName, exchangeName, routingKey, cancellationToken = stoppingToken)

            return queue
        }

