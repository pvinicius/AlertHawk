﻿namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationTeams
    {
        public int NotificationId { get; set; }
        public required string WebHookUrl { get; set; }
    }
}