namespace ShotYourPet.Messaging

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open RabbitMQ.Client
open ShotYourPet.Database
open ShotYourPet.Messaging.Interfaces
open ShotYourPet.Messaging.Listeners
open ShotYourPet.Messaging.DbUtils

module private Client =
    type MessageScopedService
        (
            connection: IConnection,
            logger: ILogger,
            eventQueue: EventQueue,
            userRpcClient: UserRpcClient,
            timelineDbContext: TimelineDbContext
        ) =
        let cancellationTokenSource = new CancellationTokenSource()

        let insertToDbTask =
            task {
                let token = cancellationTokenSource.Token
                let reader = eventQueue.NewPublicationChannel.Reader

                while not token.IsCancellationRequested do
                    let! newPublication = reader.ReadAsync()

                    do!
                        timelineDbContext.AddPost(
                            userRpcClient,
                            newPublication.Id,
                            newPublication.AuthorId,
                            newPublication.ChallengeId,
                            newPublication.Content,
                            newPublication.Date,
                            newPublication.ImageId,
                            token
                        )
            }

        member this.StopAsync token =
            task { do! connection.CloseAsync(cancellationToken = token) }

        interface IAsyncDisposable with
            member this.DisposeAsync() =
                task {
                    cancellationTokenSource.Dispose()
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


    let getUserQueue (rabbitMqConfiguration: IConfiguration) (channel: IChannel) (stoppingToken: CancellationToken) =
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
                UserRpcClient(channel, exchangeName, queryQueue.QueueName, respondQueue.QueueName)

            let! _ = channel.BasicConsumeAsync(respondQueue.QueueName, false, rpcClient, stoppingToken)

            return rpcClient
        }
