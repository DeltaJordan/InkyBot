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

        private static bool loadingNsfwMessages;

        [Command("cacheme")]
        public async Task CacheMeAsync(CommandContext context)
        {
            DiscordChannel[] markovChannels = context.Guild.Channels.Values.Where(x => x.Type == ChannelType.Text && !Settings.Instance.MarkovChannelBlacklist.Contains(x.Id)).ToArray();

            DiscordMessage statusMessage = 
                await context.RespondAsync($"Initializing your markov model. This will take a while. 0 out of {markovChannels.Length} models completed.").SafeAsync();

            int completed = 0;

            foreach (DiscordChannel channel in markovChannels)
            {
                await CacheRecursiveAsync(channel, null).SafeAsync();

                await statusMessage.ModifyAsync($"Initializing your markov model. This will take a while. {++completed} out of {markovChannels.Length} models completed.").SafeAsync();
            }

            await statusMessage.ModifyAsync("Finished!").SafeAsync();
        }

        [Command("blacklist"), RequireOwner]
        public async Task UpdateBlacklistAsync(CommandContext context, DiscordChannel channel)
        {
            Settings.Instance.MarkovChannelBlacklist = new List<ulong> { channel.Id }.Concat(Settings.Instance.MarkovChannelBlacklist).ToArray();
            Settings.Instance.Save();
        }

        [Command("nsfwmarkov")]
        public async Task NsfwMarkov(CommandContext context)
        {
            if (loadingNsfwMessages || context.Channel.Id != 422155933335158784) // #nsfw
            {
                return;
            }

            string channelFolder = Path.Combine(Globals.AppPath, "Message Log", "Channels", context.Channel.Id.ToString());
            Directory.CreateDirectory(channelFolder);

            if (Directory.GetFiles(channelFolder).Length < 10)
            {
                loadingNsfwMessages = true;

                DiscordMessage statusMessage =
                await context.RespondAsync("Initializing nsfw channel markov model. This will take a little while.").SafeAsync();

                await CacheRecursiveAsync(context.Channel, null).SafeAsync();

                await statusMessage.ModifyAsync("Finished!").SafeAsync();

                loadingNsfwMessages = false;
            }

            List<string> messages = new List<string>();
            foreach (string file in Directory.EnumerateFiles(channelFolder))
            {
                string messageJson = await File.ReadAllTextAsync(file).SafeAsync();
                DiscordMessageModel messageModel = JsonConvert.DeserializeObject<DiscordMessageModel>(messageJson);
                messages.Add(messageModel.Message);
            }

            string result = string.Empty;
            int retries = 0;

            while (result.Length < 25)
            {
                retries++;
                if (retries > 100)
                {
                    await context.RespondAsync("Failed to create a unique message within 100 tries.").SafeAsync();
                    return;
                }

                StringMarkov model = new(2);
                model.EnsureUniqueWalk = true;
                model.Learn(messages);
                result = model.Walk(10).FirstOrDefault(x =>
                    !x.Contains("||", StringComparison.InvariantCultureIgnoreCase) && // Spoilers
                    !x.Contains("<", StringComparison.InvariantCultureIgnoreCase) && // mentions, emotes, etc
                    !messages.Any(y => y.DamerauLevenshteinDistanceTo(x) < 10) && // Dupe checking
                    !x.Contains("http", StringComparison.InvariantCultureIgnoreCase) && // Links
                    x.Length >= 25) ?? string.Empty;

                result = Formatter.Sanitize(result);
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

            string result = await GetMarkovResultAsync(context, user.Select(x => x.Id).ToArray()).SafeAsync();

            await context.RespondAsync(result).SafeAsync();
        }

        private async Task UserMarkovAsync(CommandContext context)
        {
            await context.Channel.TriggerTypingAsync().SafeAsync();

            string result = await GetMarkovResultAsync(context, context.User.Id).SafeAsync();

            await context.RespondAsync(result).SafeAsync();
        }

        private async Task<string> GetMarkovResultAsync(CommandContext context, params ulong[] userIds)
        {

            IEnumerable<string> userFolders = userIds.Select(x => Path.Combine(Globals.AppPath, "Message Log", x.ToString()));

            List<string> messages = new List<string>();

            foreach (string userFolder in userFolders)
            {
                foreach (string file in Directory.EnumerateFiles(userFolder))
                {
                    string messageJson = await File.ReadAllTextAsync(file).SafeAsync();
                    DiscordMessageModel messageModel = JsonConvert.DeserializeObject<DiscordMessageModel>(messageJson);
                    messages.Add(messageModel.Message);
                }
            }

            string result = string.Empty;
            int retries = 0;

            while (result.Length < 25)
            {
                retries++;
                if (retries > 100)
                {
                    await context.RespondAsync("Failed to create a unique message within 100 tries.").SafeAsync();
                    return null;
                }

                StringMarkov model = new(2);
                model.EnsureUniqueWalk = true;
                model.Learn(messages);
                result = model.Walk(10).FirstOrDefault(x =>
                    !x.Contains("||", StringComparison.InvariantCultureIgnoreCase) && // Spoilers
                    !x.Contains("<", StringComparison.InvariantCultureIgnoreCase) && // mentions, emotes, etc
                    !messages.Any(y => y.DamerauLevenshteinDistanceTo(x) < 10) && // Dupe checking
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
                        message.MessageType != MessageType.Reply)
                    {
                        return;
                    }


                    string userFolder = Path.Combine(Globals.AppPath, "Message Log", message.Author.Id.ToString());
                    Directory.CreateDirectory(userFolder);

                    DiscordMessageModel messageModel = new()
                    {
                        Id = message.Id,
                        Message = message.Content,
                        AuthorId = message.Author.Id
                    };

                    await File.WriteAllTextAsync(Path.Combine(userFolder, message.Id + ".json"), JsonConvert.SerializeObject(messageModel)).SafeAsync();
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
