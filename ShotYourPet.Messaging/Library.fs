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
open ShotYourPet.Messaging.Interfaces
open ShotYourPet.Messaging.Listeners


type MessageService
    (configuration: IConfiguration, logger: ILogger<MessageService>, serviceScopeFactory: IServiceScopeFactory) =
    let scope = serviceScopeFactory.CreateScope()


    let mutable service: Option<MessageScopedService> = None

    interface IHostedService with
        override this.StartAsync(stoppingToken: CancellationToken) =
            logger.LogDebug "Start async"

            let rabbitMqConfiguration = configuration.GetSection "RabbitMQ"


            task {
                let connectionString =
                    match configuration.GetConnectionString("RabbitMQ") with
                    | null -> failwith "missing RabbitMQ connection string"
                    | s -> s

                let connectionFactory = ConnectionFactory()
                connectionFactory.Uri <- Uri(connectionString)
                connectionFactory.ConsumerDispatchConcurrency <- uint16 256 // TODO: thread safety (worker pattern or event bus)
                let! connection = connectionFactory.CreateConnectionAsync(cancellationToken = stoppingToken)
                let! channel = connection.CreateChannelAsync(cancellationToken = stoppingToken)

                let! publicationQueue = getPublicationQueue rabbitMqConfiguration channel stoppingToken
                let! userRpcClient = getUserQueue rabbitMqConfiguration channel stoppingToken

                let eventQueue = newEventQueue ()

                let consumer = ParsingConsumer(channel, logger, eventQueue)

                let! _ =
                    channel.BasicConsumeAsync(
                        publicationQueue.QueueName,
                        true,
                        consumer,
                        cancellationToken = stoppingToken
                    )


                let dbContext = scope.ServiceProvider.GetRequiredService<TimelineDbContext>()
                service <- Some(MessageScopedService(connection, logger, eventQueue, userRpcClient, dbContext))

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
