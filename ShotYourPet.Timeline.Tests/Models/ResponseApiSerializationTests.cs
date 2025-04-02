using System.Text.Json;
using ShotYourPet.Timeline.Model;
using Xunit;

namespace ShotYourPet.Timeline.Tests.Models;

public class ResponseApiSerializationTest
{
    [Fact]
    public void ResponseApi_ShouldSerializeToJson()
    {
        // Arrange
        var response = new ResponseApi<string>
        {
            Content = "Sample Content",
            Code = 200,
            Message = "Success"
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        Assert.Contains("\"contenu\":\"Sample Content\"", json);
        Assert.Contains("\"code\":200", json);
        Assert.Contains("\"message\":\"Success\"", json);
    }

    [Fact]
    public void ResponseApi_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = "{\"contenu\":\"Sample Content\",\"code\":200,\"message\":\"Success\"}";

        // Act
        var response = JsonSerializer.Deserialize<ResponseApi<string>>(json);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Sample Content", response.Content);
        Assert.Equal(200, response.Code);
        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public void ResponseApi_ShouldDeserializeFromJson_WithNullMessage()
    {
        // Arrange
        var json = "{\"contenu\":\"Sample Content\",\"code\":200,\"message\":null}";

        // Act
        var response = JsonSerializer.Deserialize<ResponseApi<string>>(json);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Sample Content", response.Content);
        Assert.Equal(200, response.Code);
        Assert.Null(response.Message);
    }
}