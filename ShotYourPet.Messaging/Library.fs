namespace ShotYourPet.Messaging

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open RabbitMQ.Client
open ShotYourPet.Database
open ShotYourPet.Messaging.Client


type MessageService
    (configuration: IConfiguration, logger: ILogger<MessageService>, serviceScopeFactory: IServiceScopeFactory) =
    let scope = serviceScopeFactory.CreateScope()

    let timelineDbContext =
        scope.ServiceProvider.GetRequiredService<TimelineDbContext>()


    let mutable service: Option<Client.MessageScopedService> = None

    interface IHostedService with
        override this.StartAsync(stoppingToken: CancellationToken) =
            logger.LogDebug "Start async"

            let rabbitMqConfiguration = configuration.GetSection "RabbitMQ"

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

            task {
                let connectionString =
                    match configuration.GetConnectionString("RabbitMQ") with
                    | null -> failwith "missing RabbitMQ connection string"
                    | s -> s

                let connectionFactory = ConnectionFactory()
                connectionFactory.Uri <- Uri(connectionString)
                let! connection = connectionFactory.CreateConnectionAsync(cancellationToken = stoppingToken)
                let! channel = connection.CreateChannelAsync(cancellationToken = stoppingToken)


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

                    do!
                        channel.QueueBindAsync(
                            queue.QueueName,
                            exchangeName,
                            routingKey,
                            cancellationToken = stoppingToken
                        )

                let consumer = Client.ParsingConsumer(channel, logger, timelineDbContext)

                let! _ =
                    channel.BasicConsumeAsync(queue.QueueName, true, consumer, cancellationToken = stoppingToken)
                    |> Async.AwaitTask

                service <- Some(MessageScopedService(connection, logger))

                ()
            }

        override this.StopAsync(cancellationToken) =
            task {
                match service with
                | Some s -> do! s.StopAsync(cancellationToken)
                | None -> ()
            }

    interface IAsyncDisposable with
        member this.DisposeAsync() =
            task {
                match service with
                | Some s -> do! (s :> IAsyncDisposable) |> _.DisposeAsync()
                | None -> ()
            }
            |> ValueTask
