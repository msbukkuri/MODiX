﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using Modix.Bot.Extensions;
using Modix.Data.Models.Moderation;
using Modix.Services.Moderation;
using Modix.Services.Utilities;

namespace Modix.Modules
{
    [Name("Moderation")]
    [Summary("Guild moderation commands.")]
    public class ModerationModule : ModuleBase
    {
        public ModerationModule(IModerationService moderationService)
        {
            ModerationService = moderationService;
        }

        [Command("note")]
        [Summary("Applies a note to a user's infraction history.")]
        public async Task NoteAsync(
            [Summary("The user to which the note is being applied.")]
                DiscordUserEntity subject,
            [Summary("The reason for the note.")]
            [Remainder]
                string reason)
        {
            await ModerationService.CreateInfractionAsync(Context.Guild.Id, Context.User.Id, InfractionType.Notice, subject.Id, reason, null);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("warn")]
        [Summary("Issue a warning to a user.")]
        public async Task WarnAsync(
            [Summary("The user to which the warning is being issued.")]
                DiscordUserEntity subject,
            [Summary("The reason for the warning.")]
            [Remainder]
                string reason)
        {
            await ModerationService.CreateInfractionAsync(Context.Guild.Id, Context.User.Id, InfractionType.Warning, subject.Id, reason, null);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("mute")]
        [Summary("Mute a user.")]
        public async Task MuteAsync(
            [Summary("The user to be muted.")]
                DiscordUserEntity subject,
            [Summary("The reason for the mute.")]
            [Remainder]
                string reason)
        {
            await ModerationService.CreateInfractionAsync(Context.Guild.Id, Context.User.Id, InfractionType.Mute, subject.Id, reason, null);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("tempmute")]
        [Alias("mute")]
        [Summary("Mute a user, for a temporary amount of time.")]
        public async Task TempMuteAsync(
            [Summary("The user to be muted.")]
                DiscordUserEntity subject,
            [Summary("The duration of the mute.")]
                TimeSpan duration,
            [Summary("The reason for the mute.")]
            [Remainder]
                string reason)
        {
            await ModerationService.CreateInfractionAsync(Context.Guild.Id, Context.User.Id, InfractionType.Mute, subject.Id, reason, duration);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("unmute")]
        [Summary("Remove a mute that has been applied to a user.")]
        public async Task UnMuteAsync(
            [Summary("The user to be un-muted.")]
                DiscordUserEntity subject)
        {
            await ModerationService.RescindInfractionAsync(InfractionType.Mute, subject.Id);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("ban")]
        [Alias("forceban")]
        [Summary("Ban a user from the current guild.")]
        public async Task BanAsync(
            [Summary("The user to be banned.")]
                DiscordUserEntity subject,
            [Summary("The reason for the ban.")]
            [Remainder]
                string reason)
        {
            await ModerationService.CreateInfractionAsync(Context.Guild.Id, Context.User.Id, InfractionType.Ban, subject.Id, reason, null);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("unban")]
        [Summary("Remove a ban that has been applied to a user.")]
        public async Task UnBanAsync(
            [Summary("The user to be un-banned.")]
                DiscordUserEntity subject)
        {
            await ModerationService.RescindInfractionAsync(InfractionType.Ban, subject.Id);
            await ConfirmAndReplyWithCounts(subject.Id);
        }

        [Command("clean")]
        [Summary("Mass-deletes a specified number of messages.")]
        public async Task CleanAsync(
            [Summary("The number of messages to delete.")]
                int count)
            => await ModerationService.DeleteMessagesAsync(
                Context.Channel as ITextChannel, count, true,
                    () => Context.GetUserConfirmationAsync(
                        $"You are attempting to delete the past {count} messages in #{Context.Channel.Name}.{Environment.NewLine}"));

        [Command("clean")]
        [Summary("Mass-deletes a specified number of messages.")]
        public async Task CleanAsync(
            [Summary("The number of messages to delete.")]
                int count,
            [Summary("The channel to clean.")]
                ITextChannel channel)
            => await ModerationService.DeleteMessagesAsync(
                channel, count, Context.Channel.Id == channel.Id,
                    () => Context.GetUserConfirmationAsync(
                        $"You are attempting to delete the past {count} messages in #{Context.Channel.Name}.{Environment.NewLine}"));

        [Command("clean")]
        [Summary("Mass-deletes a specified number of messages by the supplied user.")]
        public async Task CleanAsync(
            [Summary("The number of messages to delete.")]
                int count,
            [Summary("The user whose messages should be deleted.")]
                IGuildUser user)
            => await ModerationService.DeleteMessagesAsync(
                Context.Channel as ITextChannel, user, count,
                    () => Context.GetUserConfirmationAsync(
                        $"You are attempting to delete the past {count} messages by {user.Nickname ?? $"{user.Username}#{user.Discriminator}"} in #{Context.Channel.Name}.{Environment.NewLine}"));

        private async Task ConfirmAndReplyWithCounts(ulong userId)
        {
            await Context.AddConfirmation();

            var counts = await ModerationService.GetInfractionCountsForUserAsync(userId);

            //TODO: Make this configurable
            if (counts.Values.Any(count => count >= 3))
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithTitle("Infraction Count Notice")
                    .WithColor(Color.Orange)
                    .WithDescription(FormatUtilities.FormatInfractionCounts(counts))
                    .Build());
            }
        }

        internal protected IModerationService ModerationService { get; }
    }
}
