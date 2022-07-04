using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using InkyBot.Commands.Data;
using Newtonsoft.Json;
using NLog;

namespace InkyBot.Commands
{
    [Group("af"), RequireOwner]
    public class ArtFightModule : BaseCommandModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Command("init")]
        public async Task InitializeArtFightEmbedAsync(CommandContext ctx, string teamA, string teamB)
        {
            try
            {
                Dictionary<ulong, ArtFightMember> artFightMembers = new();

                File.WriteAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"), JsonConvert.SerializeObject(artFightMembers));
                File.WriteAllText(Path.Combine(Globals.AppPath, $"artfightteams{DateTime.Now.Year}.dat"), $"{teamA}\n{teamB}");

                DiscordEmbedBuilder embedBuilder = new();
                embedBuilder.Title = $"Art Fight {DateTime.Now.Year}";
                embedBuilder.Description = $"{teamA} vs. {teamB}";
                embedBuilder.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = "https://artfight.net/images/logo40.png",
                    Width = 40,
                    Height = 40
                };
                embedBuilder.AddField($"__**Team {teamA}**__", "empty", true);
                embedBuilder.AddField($"__**Team {teamB}**__", "empty", true);
                embedBuilder.Timestamp = DateTime.Now;

                DiscordMessage message = await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build()).SafeAsync();

                File.WriteAllText(Path.Combine(Globals.AppPath, $"artfightmessage{DateTime.Now.Year}.dat"), $"{message.Id}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        [Command("put")]
        public async Task PutMemberAsync(CommandContext ctx, DiscordMember member, string team, string link)
        {
            string[] teams = File.ReadAllLines(Path.Combine(Globals.AppPath, $"artfightteams{DateTime.Now.Year}.dat"));
            string teamA = teams[0];
            string teamB = teams[1];

            if (team == "1")
            {
                team = teamA;
            }
            else if (team == "2")
            {
                team = teamB;
            }
            else if (team != teamA && team != teamB)
            {
                await ctx.RespondAsync("Invalid team name.").SafeAsync();
                return;
            }

            string artFightJson = File.ReadAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"));
            Dictionary<ulong, ArtFightMember> artFightMembers = JsonConvert.DeserializeObject<Dictionary<ulong, ArtFightMember>>(artFightJson);
            artFightMembers[member.Id] = new ArtFightMember
            {
                DiscordId = member.Id,
                Team = team,
                ProfileLink = link
            };

            ulong messageId = ulong.Parse(File.ReadAllText(Path.Combine(Globals.AppPath, $"artfightmessage{DateTime.Now.Year}.dat")));
            DiscordMessage editMessage = await ctx.Channel.GetMessageAsync(messageId).SafeAsync();
            await editMessage.ModifyAsync(UpdateEmbed(artFightMembers, teamA, teamB)).SafeAsync();

            File.WriteAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"), JsonConvert.SerializeObject(artFightMembers));
        }

        [Command("putbatch")]
        public async Task PutBatchMembersAsync(CommandContext ctx, [RemainingText] string contents)
        {
            try
            {
                string artFightJson = File.ReadAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"));
                Dictionary<ulong, ArtFightMember> artFightMembers = JsonConvert.DeserializeObject<Dictionary<ulong, ArtFightMember>>(artFightJson);

                string[] teams = File.ReadAllLines(Path.Combine(Globals.AppPath, $"artfightteams{DateTime.Now.Year}.dat"));
                string teamA = teams[0];
                string teamB = teams[1];

                foreach (string line in contents.Split('\n').Skip(1))
                {
                    string[] args = line.Split(' ');
                    ulong id = ulong.Parse(args[0]);
                    string team = args[1];
                    string link = args[2];

                    if (team == "1")
                    {
                        team = teamA;
                    }
                    else if (team == "2")
                    {
                        team = teamB;
                    }
                    else if (team != teamA && team != teamB)
                    {
                        await ctx.RespondAsync("Invalid team name.").SafeAsync();
                        return;
                    }

                    artFightMembers[id] = new ArtFightMember
                    {
                        DiscordId = id,
                        Team = team,
                        ProfileLink = link
                    };
                }

                ulong messageId = ulong.Parse(File.ReadAllText(Path.Combine(Globals.AppPath, $"artfightmessage{DateTime.Now.Year}.dat")));
                DiscordMessage editMessage = await ctx.Channel.GetMessageAsync(messageId).SafeAsync();
                await editMessage.ModifyAsync(UpdateEmbed(artFightMembers, teamA, teamB)).SafeAsync();

                File.WriteAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"), JsonConvert.SerializeObject(artFightMembers));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        [Command("delete")]
        public async Task DeleteMemberAsync(CommandContext ctx, ulong id)
        {
            string[] teams = File.ReadAllLines(Path.Combine(Globals.AppPath, $"artfightteams{DateTime.Now.Year}.dat"));
            string teamA = teams[0];
            string teamB = teams[1];

            string artFightJson = File.ReadAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"));
            Dictionary<ulong, ArtFightMember> artFightMembers = JsonConvert.DeserializeObject<Dictionary<ulong, ArtFightMember>>(artFightJson);

            if (artFightMembers.ContainsKey(id))
                artFightMembers.Remove(id);

            ulong messageId = ulong.Parse(File.ReadAllText(Path.Combine(Globals.AppPath, $"artfightmessage{DateTime.Now.Year}.dat")));
            DiscordMessage editMessage = await ctx.Channel.GetMessageAsync(messageId).SafeAsync();
            await editMessage.ModifyAsync(UpdateEmbed(artFightMembers, teamA, teamB)).SafeAsync();

            File.WriteAllText(Path.Combine(Globals.AppPath, $"artfight{DateTime.Now.Year}.json"), JsonConvert.SerializeObject(artFightMembers));
        }

        private static DiscordEmbed UpdateEmbed(Dictionary<ulong, ArtFightMember> artFightMembers, string teamA, string teamB)
        {
            DiscordEmbedBuilder embedBuilder = new();
            embedBuilder.Title = $"Art Fight {DateTime.Now.Year}";
            embedBuilder.Description = $"{teamA} vs. {teamB}";
            embedBuilder.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = "https://artfight.net/images/logo40.png",
                Width = 40,
                Height = 40
            };

            string prettyTeamA = string.Join('\n', artFightMembers.Values.Where(x => x.Team == teamA).Select(x => $"[<@{x.DiscordId}>'s Profile]({x.ProfileLink})"));
            prettyTeamA = string.IsNullOrWhiteSpace(prettyTeamA) ? "empty" : prettyTeamA;
            embedBuilder.AddField($"__**Team {teamA}**__", prettyTeamA, true);

            string prettyTeamB = string.Join('\n', artFightMembers.Values.Where(x => x.Team == teamB).Select(x => $"[<@{x.DiscordId}>'s Profile]({x.ProfileLink})"));
            prettyTeamB = string.IsNullOrWhiteSpace(prettyTeamB) ? "empty" : prettyTeamB;
            embedBuilder.AddField($"__**Team {teamB}**__", prettyTeamB, true);

            embedBuilder.Timestamp = DateTime.Now;
            return embedBuilder.Build();
        }
    }
}
