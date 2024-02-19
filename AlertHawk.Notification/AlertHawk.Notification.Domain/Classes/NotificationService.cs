﻿using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;

namespace AlertHawk.Notification.Domain.Classes
{
    public class NotificationService : INotificationService
    {
        private readonly IMailNotifier _mailNotifier;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ITeamsNotifier _teamsNotifier;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(IMailNotifier mailNotifier, ISlackNotifier slackNotifier,
            ITeamsNotifier teamsNotifier, ITelegramNotifier telegramNotifier,
            INotificationRepository notificationRepository)
        {
            _mailNotifier = mailNotifier;
            _slackNotifier = slackNotifier;
            _teamsNotifier = teamsNotifier;
            _telegramNotifier = telegramNotifier;
            _notificationRepository = notificationRepository;
        }

        public async Task<bool> Send(NotificationSend notificationSend)
        {
            switch (notificationSend.NotificationTypeId)
            {
                case 1: // Email SMTP
                    notificationSend.NotificationEmail.Body += notificationSend.Message;
                    var result = await _mailNotifier.Send(notificationSend.NotificationEmail);
                    return result;

                case 2: // MS Teams
                    await _teamsNotifier.SendNotification(notificationSend.Message,
                        notificationSend.NotificationTeams.WebHookUrl);
                    return true;

                case 3: // Telegram
                    await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId,
                        notificationSend.Message,
                        notificationSend.NotificationTelegram.TelegramBotToken);
                    return true;

                case 4: // Slack
                    await _slackNotifier.SendNotification(notificationSend.NotificationSlack.Channel,
                        notificationSend.Message, notificationSend.NotificationSlack.WebHookUrl);
                    return true;
                default:
                    Console.WriteLine($"Not found NotificationTypeId: {notificationSend.NotificationTypeId}");
                    break;
            }

            return false;
        }

        public async Task InsertNotificationItem(NotificationItem notificationItem)
        {
            switch (notificationItem.NotificationTypeId)
            {
                case 1: // Email SMTP 
                    await _notificationRepository.InsertNotificationItemEmailSmtp(notificationItem);
                    break;
                case 2: // MS Teams
                    await _notificationRepository.InsertNotificationItemMsTeams(notificationItem);
                    break;
                case 3: // Telegram
                    await _notificationRepository.InsertNotificationItemTelegram(notificationItem);
                    break;
                case 4: // Slack
                    await _notificationRepository.InsertNotificationItemSlack(notificationItem);
                    break;
            }
        }

        public async Task UpdateNotificationItem(NotificationItem notificationItem)
        {
            await _notificationRepository.UpdateNotificationItem(notificationItem);
            await InsertNotificationItem(notificationItem);
        }

        public async Task DeleteNotificationItem(int id)
        {
            await _notificationRepository.DeleteNotificationItem(id);
        }

        public async Task<IEnumerable<NotificationItem>> SelectNotificationItemList()
        {
            return await _notificationRepository.SelectNotificationItemList();
        }

        public async Task<IEnumerable<NotificationItem>> SelectNotificationItemList(List<int> ids)
        {
            return await _notificationRepository.SelectNotificationItemList(ids);
        }

        public async Task<NotificationItem?> SelectNotificationItemById(int id)
        {
            return await _notificationRepository.SelectNotificationItemById(id);
        }
    }
}