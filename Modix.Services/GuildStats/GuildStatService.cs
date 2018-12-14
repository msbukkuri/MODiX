﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Modix.Data.Models.Core;
using Modix.Data.Repositories;
using Modix.Services.Messages.Discord;

namespace Modix.Services.GuildStats
{
    public interface IGuildStatService
    {
        /// <summary>
        /// Gets a list of GuildInfoResult objects representing the role distriution for the given guild.
        /// </summary>
        /// <param name="guild">The guild to retrieve roles/counts from</param>
        /// <returns>A list of GuildInfoResult(s), each representing a role in the guild</returns>
        Task<List<GuildRoleCount>> GetGuildMemberDistributionAsync(IGuild guild);

        /// <summary>
        /// Returns a mapping of <see cref="GuildUserEntity"/> to a count of the messages they've sent
        /// </summary>
        /// <param name="guildId">The guild to count messages for</param>
        /// <param name="after">How long before now to count messages for</param>
        Task<IReadOnlyDictionary<GuildUserEntity, int>> GetTopMessageCounts(IGuild guild);
    }

    public class GuildStatService :
        INotificationHandler<UserJoined>,
        INotificationHandler<UserLeft>,
        INotificationHandler<ChatMessageReceived>,
        IGuildStatService
    {
        private readonly IMemoryCache _cache;
        private readonly IMessageRepository _messageRepository;

        private readonly MemoryCacheEntryOptions _roleCacheEntryOptions =
            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(1));

        private readonly MemoryCacheEntryOptions _msgCountCacheEntryOptions =
            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30));

        public GuildStatService(IMemoryCache cache, IMessageRepository messageRepository)
        {
            _cache = cache;
            _messageRepository = messageRepository;
        }

        /// <summary>
        /// Create a unique key object for the cache
        /// </summary>
        private object GetKeyForGuild(IGuild guild) => new { guild, Target = "GuildInfo" };

        /// <summary>
        /// Create a unique key object for the cache
        /// </summary>
        private object GetKeyForMsgCounts(IGuild guild) => new { guild, Target = "MessageCounts" };

        /// <summary>
        /// Clear the cache entry for the given guild
        /// </summary>
        public void ClearCacheEntry(IGuild guild)
        {
            _cache.Remove(GetKeyForGuild(guild));
        }

        public Task Handle(ChatMessageReceived notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task Handle(UserJoined notification, CancellationToken cancellationToken)
        {
            ClearCacheEntry(notification.Guild);
            return Task.CompletedTask;
        }

        public Task Handle(UserLeft notification, CancellationToken cancellationToken)
        {
            ClearCacheEntry(notification.Guild);
            return Task.CompletedTask;
        }

        /// <inheritDoc />
        public async Task<IReadOnlyDictionary<GuildUserEntity, int>> GetTopMessageCounts(IGuild guild)
        {
            var key = GetKeyForMsgCounts(guild);

            if (!_cache.TryGetValue(key, out IReadOnlyDictionary<GuildUserEntity, int> ret))
            {
                ret = await _messageRepository.GetPerUserMessageCounts(guild.Id, TimeSpan.FromDays(30));
                _cache.Set(key, ret, _msgCountCacheEntryOptions);
            }

            return ret;
        }

        /// <inheritDoc />
        public async Task<List<GuildRoleCount>> GetGuildMemberDistributionAsync(IGuild guild)
        {
            var key = GetKeyForGuild(guild);

            if (!_cache.TryGetValue(key, out List<GuildRoleCount> ret))
            {
                //Get all the server roles once, and memoize it
                var serverRoles = guild.Roles.ToDictionary(d => d.Id, d => d);

                var members = await guild.GetUsersAsync();

                //Group the users by their highest priority role (if they have one)
                var groupings = members.GroupBy(member => GetHighestRankingRole(serverRoles, member))
                    .Where(d => d.Key != null);

                var roleCounts = groupings.OrderByDescending(d => d.Count());

                ret = roleCounts.Select(d => new GuildRoleCount
                {
                    Name = d.Key.Name,
                    Color = GetRoleColorHex(d.Key),
                    Count = d.Count()
                }).ToList();

                //Doesn't work great
                //ret.Add(new GuildInfoResult { Name = "Other", Color = "#808080", Count = members.Count - ret.Sum(d => d.Count) });

                _cache.Set(key, ret, _roleCacheEntryOptions);
            }

            return ret;
        }

        public string GetRoleColorHex(IRole role)
        {
            var ret = "99aab5"; //"Discord Grey"

            if (role.Color.RawValue > 0)
            {
                ret = role.Color.RawValue.ToString("X");
            }

            return $"#{ret}";
        }

        /// <summary>
        /// Get the user's highest position role
        /// </summary>
        /// <param name="serverRoles">A dictionary of role IDs to roles in the server</param>
        /// <returns>The highest position role</returns>
        private IRole GetHighestRankingRole(IDictionary<ulong, IRole> serverRoles, IGuildUser user)
        {
            //Get the user's role from the cache
            var roles = user.RoleIds.Select(role => serverRoles[role]);

            //Try to get their highest role
            var highestPosition = roles
                .Where(d => d.Name != "@everyone" && !d.IsManaged)
                .OrderByDescending(role => role.IsHoisted)
                .ThenByDescending(role => role.Position)
                .ThenByDescending(d => !d.Color.Equals(Color.Default))
                .FirstOrDefault();

            return highestPosition;
        }
    }
}
