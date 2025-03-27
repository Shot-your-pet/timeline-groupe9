module ShotYourPet.Messaging.Listeners

open System
open System.Collections.Concurrent
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open RabbitMQ.Client
open ShotYourPet.Database
open ShotYourPet.Messaging.Interfaces
open ShotYourPet.Messaging.Model
open ShotYourPet.Messaging.DbUtils

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

and UserRpcClient(channel: IChannel, queryExchange: string, queryQueue: string, respondQueue: string) =
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
