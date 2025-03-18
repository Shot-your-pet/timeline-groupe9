namespace ShotYourPet.Messaging

open System
open System.Collections.Concurrent
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open RabbitMQ.Client
open ShotYourPet.Database
open ShotYourPet.Messaging.Interfaces
open ShotYourPet.Messaging.Model
open ShotYourPet.Messaging.DbUtils

module private Client =
    type ParsingConsumer
        (channel: IChannel, logger: ILogger, timelineDbContext: TimelineDbContext, userRpcClient: UserRpcClient) =
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
                                userRpcClient,
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

    and UserRpcClient
        (channel: IChannel, queryExchange: string, queryQueue: string, respondQueue: string, logger: ILogger) =
        inherit AsyncDefaultBasicConsumer(channel)

        let callbackMap =
            ConcurrentDictionary<string, TaskCompletionSource<UserInformation>>()

        override this.HandleBasicDeliverAsync(_, tag, _, _, _, props, body, cancellationToken) =
            match props.CorrelationId |> Option.ofObj with
            | Some correlationId when correlationId.Length <> 0 ->
                let found, value = callbackMap.TryRemove(correlationId)

                if found then
                    try
                        match JsonSerializer.Deserialize<UserInformation>(body.Span) |> Option.ofObj with
                        | Some info -> value.SetResult(info)
                        | None -> failwith "Missing user information"
                    with e ->
                        value.SetException e
            | _ -> ()

            task { do! channel.BasicAckAsync(tag, false, cancellationToken) }

        interface IUserRpcClient with
            member this.QueryInformationAsync userId cancellationToken =
                task {
                    let correlationKey = Guid.NewGuid().ToString()

                    let props = BasicProperties(CorrelationId = correlationKey, ReplyTo = respondQueue)

                    let completionSource =
                        TaskCompletionSource<UserInformation>(TaskCreationOptions.RunContinuationsAsynchronously)

                    if not (callbackMap.TryAdd(correlationKey, completionSource)) then
                        failwith "Failed to register callback for result"

                    let content =
                        JsonSerializer.SerializeToUtf8Bytes<UserQuery>({ IdKeycloak = userId })

                    do!
                        channel.BasicPublishAsync(
                            queryExchange,
                            queryQueue,
                            body = content,
                            cancellationToken = cancellationToken,
                            mandatory = true,
                            basicProperties = props
                        )

                    use _ =
                        cancellationToken.Register(fun () ->
                            callbackMap.TryRemove correlationKey |> ignore
                            completionSource.SetCanceled())

                    return! completionSource.Task
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
                |> Option.defaultValue "publication.publication_events"

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


    let getUserQueue
        (rabbitMqConfiguration: IConfiguration)
        (channel: IChannel)
        (stoppingToken: CancellationToken)
        (logger: ILogger)
        =
        let exchangeName =
            rabbitMqConfiguration["USER_INFO_EXCHANGE"]
            |> Option.ofObj
            |> Option.defaultValue ""

        let queueName =
            rabbitMqConfiguration["USER_INFO_QUEUE"]
            |> Option.ofObj
            |> Option.defaultValue "utilisateurs.infos_utilisateur"

        task {
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

            let! queryQueue =
                channel.QueueDeclareAsync(
                    queue = queueName,
                    durable = true,
                    exclusive = false,
                    autoDelete = false,
                    passive = true,
                    cancellationToken = stoppingToken
                )

            let! respondQueue =
                channel.QueueDeclareAsync(
                    queue = "",
                    durable = false,
                    exclusive = false,
                    autoDelete = false,
                    passive = false,
                    cancellationToken = stoppingToken
                )

            if exchangeName <> "" then
                do! channel.QueueBindAsync(respondQueue.QueueName, exchangeName, "", cancellationToken = stoppingToken)

            let rpcClient =
                UserRpcClient(channel, exchangeName, queryQueue.QueueName, respondQueue.QueueName, logger)

            let! _ = channel.BasicConsumeAsync(respondQueue.QueueName, false, rpcClient, stoppingToken)

            return rpcClient
        }
