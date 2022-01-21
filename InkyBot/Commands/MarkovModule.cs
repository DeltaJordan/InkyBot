using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using InkyBot.Algorithms;
using InkyBot.Models;
using MarkovSharp.TokenisationStrategies;
using Newtonsoft.Json;
using NLog;

namespace InkyBot.Commands
{
    public sealed class MarkovModule : BaseCommandModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DiscordMessageContext databaseContext;

        public MarkovModule(DiscordMessageContext databaseContext)
        {
            this.databaseContext = databaseContext;
        }

        [Command("cacheme"), RequireOwner]
        public async Task CacheMeAsync(CommandContext context, DiscordChannel reqChannel = null)
        {
            DiscordChannel[] markovChannels;
            if (reqChannel == null)
                markovChannels = context.Guild.Channels.Values.Where(x => x.Type == ChannelType.Text).ToArray();
            else
                markovChannels = new DiscordChannel[] { reqChannel };

            DiscordMessage statusMessage = 
                await context.RespondAsync($"Initializing your markov model. This will take a while. 0 out of {markovChannels.Length} models completed.").SafeAsync();

            int completed = 0;

            foreach (DiscordChannel channel in markovChannels)
            {
                await CacheRecursiveAsync(channel, null).SafeAsync();

                await statusMessage.ModifyAsync($"Initializing your markov model. This will take a while. {++completed} out of {markovChannels.Length} models completed.").SafeAsync();

                try
                {
                    await databaseContext.SaveChangesAsync().SafeAsync();
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex);
                }
            }

            await statusMessage.ModifyAsync("Finished!").SafeAsync();
        }

        [Command("blacklist"), RequireOwner]
        public async Task UpdateBlacklistAsync(CommandContext context, DiscordChannel channel)
        {
            Settings.Instance.MarkovChannelBlacklist = new List<ulong> { channel.Id }.Concat(Settings.Instance.MarkovChannelBlacklist).ToArray();
            Settings.Instance.Save();

            await context.RespondAsync($"Added channel {channel.Mention} to blacklist successfully!").SafeAsync();
        }

        [Command("servermarkov")]
        public async Task ServerMarkovAsync(CommandContext context)
        {
            var discordMessageItems = databaseContext.MessageItems.Where(x => !Settings.Instance.MarkovChannelBlacklist.Contains(x.Id));

            List<string> messages = discordMessageItems.Select(x => x.Message).ToList();

            string result = GetMarkovFromLines(messages);

            if (string.IsNullOrEmpty(result))
            {
                await context.RespondAsync("Failed to generate unique markov in 100 tries.").SafeAsync();
                return;
            }

            await context.RespondAsync(result).SafeAsync();
        }

        [Command("channelmarkov")]
        public async Task ChannelMarkovAsync(CommandContext context, DiscordChannel channel)
        {
            var discordMessageItems = databaseContext.MessageItems.Where(x => x.ChannelId == channel.Id);

            List<string> messages = discordMessageItems.Select(x => x.Message).ToList();

            string result = GetMarkovFromLines(messages);

            if (string.IsNullOrEmpty(result))
            {
                await context.RespondAsync("Failed to generate unique markov in 100 tries.").SafeAsync();
                return;
            }

            await context.RespondAsync(result).SafeAsync();
        }

        [Command("nsfwmarkov")]
        public async Task NsfwMarkovAsync(CommandContext context)
        {
            if (context.Channel.Id != 422155933335158784) // #nsfw
            {
                return;
            }

            IQueryable<DiscordMessageItem> discordMessageItems = databaseContext.MessageItems.Where(x => x.ChannelId == 422155933335158784);

            List<string> messages = discordMessageItems.Select(x => x.Message).ToList();

            string result = GetMarkovFromLines(messages);

            if (string.IsNullOrEmpty(result))
            {
                await context.RespondAsync("Failed to generate unique markov in 100 tries.").SafeAsync();
                return;
            }

            await context.RespondAsync(result).SafeAsync();
        }

        [Command("usermarkov")]
        public async Task UserMarkovAsync(CommandContext context, params DiscordUser[] user)
        {
            if (user.Length == 0)
            {
                await UserMarkovAsync(context).SafeAsync();
                return;
            }

            await context.Channel.TriggerTypingAsync().SafeAsync();

            string result = GetUserMarkovResult(context, user.Select(x => x.Id).ToArray());

            if (string.IsNullOrEmpty(result))
            {
                await context.RespondAsync("Failed to generate unique markov in 100 tries.").SafeAsync();
                return;
            }

            await context.RespondAsync(result).SafeAsync();
        }

        private async Task UserMarkovAsync(CommandContext context)
        {
            await context.Channel.TriggerTypingAsync().SafeAsync();

            string result = GetUserMarkovResult(context, context.User.Id);

            if (string.IsNullOrEmpty(result))
            {
                await context.RespondAsync("Failed to generate unique markov in 100 tries.").SafeAsync();
                return;
            }

            await context.RespondAsync(result).SafeAsync();
        }

        private string GetUserMarkovResult(CommandContext context, params ulong[] userIds)
        {
            IQueryable<DiscordMessageItem> discordMessageItems = databaseContext.MessageItems
                .Where(x => !Settings.Instance.MarkovChannelBlacklist.Contains(x.ChannelId) && userIds.Contains(x.AuthorId));

            List<string> messages = discordMessageItems.Select(x => x.Message).ToList();

            return GetMarkovFromLines(messages);
        }

        private string GetMarkovFromLines(IEnumerable<string> lines)
        {
            string result = string.Empty;
            int retries = 0;

            while (result.Length < 25)
            {
                retries++;
                if (retries > 100)
                {
                    return null;
                }

                StringMarkov model = new(2);
                model.EnsureUniqueWalk = true;
                model.Learn(lines);
                result = model.Walk(10).FirstOrDefault(x =>
                    !x.Contains("||", StringComparison.InvariantCultureIgnoreCase) && // Spoilers
                    !x.Contains("<", StringComparison.InvariantCultureIgnoreCase) && // mentions, emotes, etc
                    !lines.Any(y => y.DamerauLevenshteinDistanceTo(x) < 10) && // Dupe checking
                    !x.Contains("http", StringComparison.InvariantCultureIgnoreCase) && // Links
                    x.Length >= 25) ?? string.Empty;

                result = Formatter.Sanitize(result);
            }

            return result;
        }

        private async Task CacheRecursiveAsync(DiscordChannel channel, DiscordMessage oldestMessage)
        {
            try
            {
                IReadOnlyList<DiscordMessage> channelMessages;

                if (oldestMessage != null)
                    channelMessages = await channel.GetMessagesBeforeAsync(oldestMessage.Id).SafeAsync();
                else
                    channelMessages = await channel.GetMessagesAsync().SafeAsync();

                if (channelMessages.Count == 0)
                {
                    return;
                }

                foreach (var message in channelMessages)
                {
                    if (message.MessageType != MessageType.Default &&
                        message.MessageType != MessageType.Reply ||
                        databaseContext.MessageItems.Find(message.Id) != null)
                    {
                        return;
                    }

                    DiscordMessageItem messageModel = new()
                    {
                        Id = message.Id,
                        Message = message.Content,
                        AuthorId = message.Author.Id,
                        ChannelId = channel.Id,
                    };

                    databaseContext.MessageItems.Add(messageModel);
                }

                DiscordMessage referenceMessage = channelMessages.OrderBy(x => x.Timestamp).FirstOrDefault();
                if (referenceMessage != null)
                {
                    await CacheRecursiveAsync(channel, referenceMessage).SafeAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to get channel messages for channel {channel.Name}.");
            }
        }
    }
}
