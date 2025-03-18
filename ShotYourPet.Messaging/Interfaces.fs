namespace ShotYourPet.Messaging

open System
open System.Threading
open System.Threading.Tasks
open ShotYourPet.Messaging.Model

module Interfaces =
    type IUserRpcClient =
        abstract member QueryInformationAsync:
            userId: Guid -> cancellationToken: CancellationToken -> Task<UserInformation>
