namespace ShotYourPet.Messaging

open System
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open ShotYourPet.Messaging.Model

module Interfaces =
    type IUserRpcClient =
        abstract member QueryInformationAsync:
            userId: Guid -> cancellationToken: CancellationToken -> Task<UserInformation>


    type EventQueue =
        { NewPublicationChannel: Channel<NewPublication> }

    let newEventQueue () =
        { NewPublicationChannel = Channel.CreateUnbounded() }

    let postPublicationEvent eventQueue publication =
        task { do! eventQueue.NewPublicationChannel.Writer.WriteAsync(publication) }
