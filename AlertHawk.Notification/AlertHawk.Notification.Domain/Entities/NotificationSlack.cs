﻿namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationSlack
    {
        public int NotificationId { get; set; }
        public string ChannelName { get; set; }
        public string WebHookUrl { get; set; }
    }
}