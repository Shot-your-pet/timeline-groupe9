module ShotYourPet.Messaging.DbUtils

open System
open System.Runtime.CompilerServices
open System.Threading
open Microsoft.FSharp.Core
open ShotYourPet.Database

type internal TimelineDbContextExt =
    [<Extension>]
    static member FindOrCreateAuthorAsync
        (timelineDbContext: TimelineDbContext, authorId: Guid, cancellationToken: CancellationToken)
        =
        task {
            let! maybeAuthor = timelineDbContext.Authors.FindAsync(authorId, cancellationToken)

            match maybeAuthor with
            | null ->
                let entity = timelineDbContext.Authors.Add(Author(Id = authorId))
                return entity.Entity
            | s -> return s
        }

    [<Extension>]
    static member AddPost
        (
            timelineDbContext: TimelineDbContext,
            postId: int64,
            authorId: Guid,
            challengeId: int64,
            content: string | null,
            publishedAt: DateTimeOffset,
            imageId: int64,
            cancellationToken: CancellationToken
        ) =
        task {
            let! existing = timelineDbContext.Posts.FindAsync(postId, cancellationToken)

            if existing = null then
                let! author = timelineDbContext.FindOrCreateAuthorAsync(authorId, cancellationToken)

                timelineDbContext.Posts.AddRange(
                    Post(
                        Id = postId,
                        Author = author,
                        ChallengeId = challengeId,
                        Content = content,
                        PublishedAt = publishedAt,
                        ImageId = imageId
                    )
                )

                let! _ = timelineDbContext.SaveChangesAsync(cancellationToken)

                ()
        }
