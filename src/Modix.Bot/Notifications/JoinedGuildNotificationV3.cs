﻿using Discord.WebSocket;
using MediatR;

namespace Modix.Bot.Notifications;

public class JoinedGuildNotificationV3(SocketGuild guild) : INotification
{
    public SocketGuild Guild { get; } = guild;
}