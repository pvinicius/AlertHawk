using AlertHawk.Notification.Controllers;
using AlertHawk.Notification.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Notification.Tests.NotifierTests;

public class NotifierTests : IClassFixture<NotificationController>
{
    private readonly NotificationController _notificationController;

    public NotifierTests(NotificationController notificationController)
    {
        _notificationController = notificationController;
    }

    [Fact]
    public async Task Should_Send_Telegram_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 3, // Telegram
            NotificationTelegram = new NotificationTelegram
            {
                ChatId = GlobalVariables.TelegramChatId,
                NotificationId = 1,
                TelegramBotToken = GlobalVariables.TelegramWebHook,
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public async Task Should_Send_EmailSmtp_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 1, // EmailSmtp
            NotificationEmail = new NotificationEmail
            {
                ToEmail = "alerthawk@outlook.com",
                FromEmail = "alerthawk@outlook.com",
                Username = "alerthawk@outlook.com",
                Password = GlobalVariables.EmailPassword,
                Hostname = "smtp.office365.com",
                Body = "Body",
                Subject = "Subject",
                Port = 587,
                EnableSsl = true,
                IsHtmlBody = false,
                NotificationId = 1
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public async Task Should_Send_EmailSmtpWithCCAndBCC_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 1, // EmailSmtp
            NotificationEmail = new NotificationEmail
            {
                ToEmail = "alerthawk@outlook.com",
                ToCCEmail = "alerthawk@outlook.com",
                ToBCCEmail = "alerthawk@outlook.com",
                FromEmail = "alerthawk@outlook.com",
                Username = "alerthawk@outlook.com",
                Password = GlobalVariables.EmailPassword,
                Hostname = "smtp.office365.com",
                Body = "Body",
                Subject = "Subject",
                Port = 587,
                EnableSsl = true,
                IsHtmlBody = false,
                NotificationId = 1
            },
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }

    [Fact]
    public async Task Should_Send_Slack_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 4, // Slack
            NotificationSlack = new NotificationSlack()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.SlackWebHookUrl,
                Channel = "alerthawk-test"
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }
    
    [Fact]
    public async Task Should_Send_Teams_Notification()
    {
        // Arrange
        var notificationSend = new NotificationSend
        {
            Message = "Test from Unit testing",
            NotificationTypeId = 2, // Teams
            NotificationTeams = new NotificationTeams()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.TeamsWebHookUrl
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }
    
    [Fact]
    public async Task Should_Send_WebHook_Notification()
    {
        // Arrange
        var body =
            "{\n    \"message\": \"Error: #statuscode - #responsemessage\",\n    \"alias\": \"Life is too short for no alias2\",\n    \"description\":\"Every alert needs a description2\",\n    \"actions\": [\"Restart\", \"AnExampleAction\"],\n    \"tags\": [\"OverwriteQuietHours\",\"Critical\"],\n    \"details\":{\"key1\":\"value1\",\"key2\":\"value2\"},\n    \"entity\":\"An example entity\",\n    \"priority\":\"P2\"\n}";
        
        var headers = "{\"Authorization\": \"GenieKey d6d191e5-f671-4768-af7c-868c2046f13d\"}";
        
        var notificationSend = new NotificationSend
        {
            Message = "Message",
            NotificationTypeId = 5, // WebHook
            NotificationWebHook = new NotificationWebHook()
            {
                NotificationId = 1,
                WebHookUrl = GlobalVariables.WebHookUrl,
                Message = "test",
                Body = body,
                HeadersJson = headers
            },
            NotificationTimeStamp = DateTime.UtcNow
        };

        // Act
        var result = await _notificationController.SendNotification(notificationSend) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(true, result.Value);
    }
}