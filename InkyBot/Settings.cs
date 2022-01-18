using Newtonsoft.Json;
using SDL.Util;

namespace InkyBot
{
    public class Settings
    {
        public static Settings Instance => instance.Value;

        private static readonly SafeLazy<Settings> instance = new(InitSettings, System.Threading.LazyThreadSafetyMode.PublicationOnly);

        public readonly string DiscordSecret;
        //config members
        public readonly string Prefix;
        
        public ulong[] MarkovChannelBlacklist { get; set; }

        private Settings(string discordSecret, ConfigFile cf)
        {
            this.DiscordSecret = discordSecret;
            this.Prefix = cf.Prefix;
            this.MarkovChannelBlacklist = cf.MarkovChannelBlacklist;
        }

        public void Save()
        {
            ConfigFile configFile = new()
            {
                Prefix = this.Prefix,
                MarkovChannelBlacklist = this.MarkovChannelBlacklist
            };

            File.WriteAllText(Path.Combine(Globals.AppPath, "config.json"), JsonConvert.SerializeObject(configFile));
        }

        [JsonObject]
        private sealed class ConfigFile
        {
            public string Prefix { get; set; }
            public ulong[] MarkovChannelBlacklist { get; set; } = new ulong[]
            {
                808813924685447188, // #vent
                422155933335158784, // #nsfw
                898001921451900939, // #bot-commands
                352966764608618496, // #announcements
                762784203438948362, // #rules
                880733987918716948, // #events
            };
        }

        private static Settings InitSettings()
        {
            string discordSecret = Environment.GetEnvironmentVariable("DISCORD_SECRET");

            if (string.IsNullOrWhiteSpace(discordSecret))
            {
                throw new Exception("Discord secret environmental variable cannot be empty or null.");
            }

            string configJson = File.ReadAllText(Path.Combine(Globals.AppPath, "config.json"));
            ConfigFile config = JsonConvert.DeserializeObject<ConfigFile>(configJson);

            return new Settings(discordSecret, config);
        }
    }
}
