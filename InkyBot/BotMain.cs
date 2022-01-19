global using SDL.Util;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using InkyBot.Models;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Reflection;

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
                Layout = "[${time}] [${level:uppercase=true}] [${logger}] ${message}"
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);


            ColoredConsoleTarget coloredConsoleTarget = new ColoredConsoleTarget
            {
                UseDefaultRowHighlightingRules = true
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

            commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
#if DEBUG
                StringPrefixes = new string[] { Settings.Instance.Prefix + Settings.Instance.Prefix },
#else
                StringPrefixes = new string[] { Settings.Instance.Prefix },
#endif
                CaseSensitive = false
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            interactivity = Client.UseInteractivity(new InteractivityConfiguration { });

            Client.MessageCreated += Client_MessageCreatedAsync;
            Client.MessageUpdated += Client_MessageUpdatedAsync;

            await Client.ConnectAsync().SafeAsync();

            await Task.Delay(-1).SafeAsync();
        }

        private static async Task Client_MessageUpdatedAsync(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs e)
        {
            if (e.Guild.Id == 254091452559130626)
            {
                if (e.Message.MessageType != MessageType.Default &&
                    e.Message.MessageType != MessageType.Reply)
                {
                    return;
                }

                string userFolder = Path.Combine(Globals.AppPath, "Message Log", e.Author.Id.ToString());
                Directory.CreateDirectory(userFolder);

                DiscordMessageModel messageModel = new()
                {
                    Id = e.Message.Id,
                    Message = e.Message.Content,
                    AuthorId = e.Author.Id
                };

                await File.WriteAllTextAsync(Path.Combine(userFolder, e.Message.Id + "_edited.json"), JsonConvert.SerializeObject(messageModel)).SafeAsync();
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

                string userFolder = Path.Combine(Globals.AppPath, "Message Log", e.Author.Id.ToString());
                Directory.CreateDirectory(userFolder);

                DiscordMessageModel messageModel = new()
                {
                    Id = e.Message.Id,
                    Message = e.Message.Content,
                    AuthorId = e.Author.Id
                };

                await File.WriteAllTextAsync(Path.Combine(userFolder, e.Message.Id + ".json"), JsonConvert.SerializeObject(messageModel)).SafeAsync();

                if (e.Channel.Id == 422155933335158784) // #nsfw
                {
                    string channelFolder = Path.Combine(Globals.AppPath, "Message Log", "Channels", e.Channel.Id.ToString());
                    Directory.CreateDirectory(channelFolder);
                    await File.WriteAllTextAsync(Path.Combine(channelFolder, e.Message.Id + ".json"), JsonConvert.SerializeObject(messageModel)).SafeAsync();
                }
            }
        }
    }
}