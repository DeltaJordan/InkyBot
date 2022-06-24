global using SDL.Util;
using System.Reflection;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using InkyBot.Algorithms.Gibberish;
using InkyBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using Npgsql;

namespace InkyBot
{
    public static class BotMain
    {
        public static DiscordClient Client { get; private set; }

        private static CommandsNextExtension commands;
        private static InteractivityExtension interactivity;

        public static async Task Main()
        {
            // Make sure Log folder exists
            Directory.CreateDirectory(Path.Combine(Globals.AppPath, "Logs"));

            // Checks for existing latest log
            if (File.Exists(Path.Combine(Globals.AppPath, "Logs", "latest.log")))
            {
                // This is no longer the latest log; move to backlogs
                string oldLogFileName = File.ReadAllLines(Path.Combine(Globals.AppPath, "Logs", "latest.log"))[0];
                File.Move(Path.Combine(Globals.AppPath, "Logs", "latest.log"), Path.Combine(Globals.AppPath, "Logs", oldLogFileName));
            }

            // Builds a file name to prepare for future backlogging
            string logFileName = $"{DateTime.Now:dd-MM-yy}-1.log";

            // Loops until the log file doesn't exist
            for (int index = 2; File.Exists(Path.Combine(Globals.AppPath, "Logs", logFileName)); index++)
            {
                logFileName = $"{DateTime.Now:dd-MM-yy}-{index}.log";
            }

            // Logs the future backlog file name
            File.WriteAllText(Path.Combine(Globals.AppPath, "Logs", "latest.log"), $"{logFileName}\n");

            // Set up logging through NLog
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget logfile = new FileTarget("logfile")
            {
                FileName = Path.Combine(Globals.AppPath, "Logs", "latest.log"),
                Layout = "[${time}] [${level:uppercase=true}] [${logger}] ${message}${when:when=length('${exception}')>0:Inner=\n}${exception:format=tostring}"
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);


            ColoredConsoleTarget coloredConsoleTarget = new ColoredConsoleTarget
            {
                UseDefaultRowHighlightingRules = true,
                Layout = "[${time}] [${level:uppercase=true}] [${logger}] ${message}${when:when=length('${exception}')>0:Inner=\n}${exception:format=tostring}"
            };
            config.AddRule(LogLevel.Info, LogLevel.Fatal, coloredConsoleTarget);
            LogManager.Configuration = config;

            // Load the settings from file, then store it in the globals
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Settings.Instance.DiscordSecret,
                TokenType = TokenType.Bot,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information
            });

            Console.WriteLine(Settings.Instance.Prefix);

            ServiceCollection services = new();
            services.AddDbContext<DiscordMessageContext>(options => options.UseNpgsql(Settings.Instance.ConnectionString));

            commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
#if DEBUG
                StringPrefixes = new string[] { Settings.Instance.Prefix + Settings.Instance.Prefix },
#else
                StringPrefixes = new string[] { Settings.Instance.Prefix },
#endif
                CaseSensitive = false,
                Services = services.BuildServiceProvider()
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            interactivity = Client.UseInteractivity(new InteractivityConfiguration { });

            Client.MessageCreated += Client_MessageCreatedAsync;
            Client.MessageCreated += Client_HandleKeySmashAsync;
            Client.MessageUpdated += Client_MessageUpdatedAsync;

            await Client.ConnectAsync().SafeAsync();

            await Task.Delay(-1).SafeAsync();
        }

        private static async Task Client_HandleKeySmashAsync(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Guild.Id == 254091452559130626)
            {
#if DEBUG
                if (e.Author.Id != Client.CurrentApplication.Owners.First().Id)
                {
                    return;
                }
#endif
                if (Gibberish.Classify(e.Message.Content) > 0.75)
                {
                    await e.Message.RespondAsync("Yup.").SafeAsync();
                }
            }
        }

        private static async Task Client_MessageUpdatedAsync(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs e)
        {
            if (e.Guild.Id == 254091452559130626)
            {
                // TODO: Edited messages
            }
        }

        private static async Task Client_MessageCreatedAsync(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Guild.Id == 254091452559130626)
            {
                if (e.Message.MessageType != MessageType.Default &&
                    e.Message.MessageType != MessageType.Reply)
                {
                    return;
                }

                await using var conn = new NpgsqlConnection(Settings.Instance.ConnectionString);
                await conn.OpenAsync().SafeAsync();

                await using var cmd = new NpgsqlCommand("INSERT INTO \"MessageItems\" (\"Id\", \"Message\", \"AuthorId\", \"ChannelId\") VALUES ($1, $2, $3, $4)", conn)
                {
                    Parameters =
                    {
                        new() { Value = (decimal) e.Message.Id },
                        new() { Value = e.Message.Content },
                        new() { Value = (decimal) e.Author.Id },
                        new() { Value = (decimal) e.Channel.Id }
                    }
                };

                await cmd.ExecuteNonQueryAsync().SafeAsync();

                Regex regex = new(@"(^|[^a-z])vore([^a-z]|$)", RegexOptions.IgnoreCase);
                Match match = regex.Match(e.Message.Content);
                if (match.Success)
                {
                    string lastMentionedFile = Path.Combine(Globals.AppPath, "vore.dat");
                    if (File.Exists(lastMentionedFile))
                    {
                        DateTime lastMentioned = JsonConvert.DeserializeObject<DateTime>(await File.ReadAllTextAsync(lastMentionedFile).SafeAsync());
                        TimeSpan timeDifference = DateTime.Now - lastMentioned;

                        string unitsLabel;
                        if (timeDifference.TotalDays >= 365)
                        {
                            unitsLabel = "Years";
                        }
                        else if (timeDifference.TotalDays >= 30)
                        {
                            unitsLabel = "Months";
                        }
                        else if (timeDifference.Days > 0)
                        {
                            unitsLabel = "Days";
                        }
                        else if (timeDifference.Hours > 0)
                        {
                            unitsLabel = "Hours";
                        }
                        else if (timeDifference.Minutes > 0)
                        {
                            unitsLabel = "Minutes";
                        }
                        else if (timeDifference.Seconds > 0)
                        {
                            unitsLabel = "Seconds";
                        }
                        else
                        {
                            unitsLabel = "Milliseconds";
                        }

                        await e.Message.RespondAsync("0 " + unitsLabel).SafeAsync();
                    }
                    else
                    {
                        await e.Message.RespondAsync("**Day 0**").SafeAsync();
                    }

                    await File.WriteAllTextAsync(lastMentionedFile, JsonConvert.SerializeObject(DateTime.Now)).SafeAsync();
                }
            }
        }
    }
}