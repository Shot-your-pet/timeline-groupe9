namespace ShotYourPet.Messaging

open System
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Control
open RabbitMQ.Client
open ShotYourPet.Database
open ShotYourPet.Messaging.Model

module Client =
    type private ParsingConsumer(channel: IChannel, logger : ILogger<_>) =
        inherit AsyncDefaultBasicConsumer(channel)
        let onPublishEvent = Event<PublishingEvent>()

        override this.HandleBasicDeliverAsync(_, deliveryTag, _, _, routingKey, _, body, cancellationToken) =
            task {
                try
                    try
                        match routingKey with
                        | "publication" ->
                            let value = JsonSerializer.Deserialize<PublishingEvent>(body.Span)
                            onPublishEvent.Trigger value
                        | _ -> ()
                    with e ->
                        logger.LogError(e, "failed to parse message")
                finally
                    channel.BasicAckAsync(deliveryTag, false, cancellationToken).AsTask().Wait()
            }

        member this.PublishEvent: IEvent<PublishingEvent> = onPublishEvent.Publish
        
    type MessageService(configuration: IConfiguration, logger: ILogger<MessageService>, serviceScopeFactory : IServiceScopeFactory) =
        let scope = serviceScopeFactory.CreateScope()
        let timelineDbContext = scope.ServiceProvider.GetRequiredService<TimelineDbContext>()
        let mutable consumer : ParsingConsumer | null  = null
        let mutable connection : IConnection | null = null
        
        let findOrCreateAuthorAsync (authorId : Guid) =
                task {
                    let! maybeAuthor = timelineDbContext.Authors.FindAsync authorId
                    match maybeAuthor with
                        | null ->
                            let entity = timelineDbContext.Authors.Add(Author(Id = authorId)) 
                            return entity.Entity
                        | s -> return s
                }

        interface IHostedService with
            override this.StartAsync (stoppingToken: Threading.CancellationToken)=
                logger.LogDebug "Start async"
                task {
                    let connectionString =
                        match configuration.GetConnectionString("RabbitMQ") with
                        | null -> failwith "missing RabbitMQ connection string"
                        | s -> s

                    let connectionFactory = ConnectionFactory()
                    connectionFactory.Uri <- Uri(connectionString)
                    let! connection = connectionFactory.CreateConnectionAsync(cancellationToken=stoppingToken)
                    let! channel = connection.CreateChannelAsync(cancellationToken=stoppingToken)

                    let! exchange = channel.ExchangeDeclareAsync("broadcast", ExchangeType.Fanout, cancellationToken=stoppingToken)

                    let! queue = channel.QueueDeclareAsync("timeline", false, false, false, cancellationToken=stoppingToken)

                    do! channel.QueueBindAsync(queue.QueueName, "broadcast", "publication", null, cancellationToken=stoppingToken)
                    
                    consumer <- ParsingConsumer(channel, logger)
                    let! _ = channel.BasicConsumeAsync("timeline", false, consumer, cancellationToken=stoppingToken) |> Async.AwaitTask

                    while connection.IsOpen do
                        let! event = consumer.PublishEvent |> Async.AwaitEvent
                        match event.Content with
                                 | EventContent.NewPublication e ->
                                    use! transaction = timelineDbContext.Database.BeginTransactionAsync()
                                    try
                                        let! existing = timelineDbContext.Posts.FindAsync e.Id
                                        if existing = null then
                                            let! author = findOrCreateAuthorAsync e.AuthorId
                                            timelineDbContext.Posts.AddRange(Post(Id=e.Id, Author=author)) |> ignore
                                            let _ = timelineDbContext.SaveChanges()
                                            do! transaction.CommitAsync()
                                    with
                                    | e ->
                                        do! transaction.RollbackAsync()
                                        logger.LogError(e, "failed to insert ")
                                 | e -> logger.LogWarning("event {0} is not implemented", e)
                    ()
            }

            override this.StopAsync(cancellationToken) =
                 logger.LogDebug "Stop async"
                 match connection with
                 | null -> Task.CompletedTask
                 | v -> v.CloseAsync(cancellationToken)