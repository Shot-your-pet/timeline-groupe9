using System;
using System.Text.Json;
using ShotYourPet.Timeline.Model;
using Xunit;

namespace ShotYourPet.Timeline.Tests.Models;

public class AuthorSerializationTests
{
    [Fact]
    public void Author_ShouldSerializeToJson()
    {
        // Arrange
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Pseudo = "AuthorNickname",
            AvatarId = 12345
        };

        // Act
        var json = JsonSerializer.Serialize(author);

        // Assert
        Assert.Contains($"\"id\":\"{author.Id}\"", json);
        Assert.Contains("\"pseudo\":\"AuthorNickname\"", json);
        Assert.Contains("\"avatar_id\":12345", json);
    }

    [Fact]
    public void Author_ShouldDeserializeFromJson()
    {
        // Arrange
        var id = Guid.NewGuid();
        var json = $"{{\"id\":\"{id}\",\"pseudo\":\"AuthorNickname\",\"avatar_id\":12345}}";
        // Act
        var author = JsonSerializer.Deserialize<Author>(json);

        // Assert
        Assert.NotNull(author);
        Assert.Equal(id, author.Id);
        Assert.Equal("AuthorNickname", author.Pseudo);
        Assert.Equal(12345, author.AvatarId);
    }

    [Fact]
    public void Author_ShouldDeserializeFromJson_WithNullAvatarId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var json = $"{{\"id\":\"{id}\",\"pseudo\":\"AuthorNickname\",\"avatar_id\":null}}";

        // Act
        var author = JsonSerializer.Deserialize<Author>(json);

        // Assert
        Assert.NotNull(author);
        Assert.Equal(id, author.Id);
        Assert.Equal("AuthorNickname", author.Pseudo);
        Assert.Null(author.AvatarId);
    }
}