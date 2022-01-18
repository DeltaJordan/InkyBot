using System.Reflection;
using Newtonsoft.Json;

namespace InkyBot
{
    public static class Globals
    {
        //TODO: not lmao
        public const string BaseURL = "lmao/";
        public static List<ulong> SuperUsers => GetSuperUsers();

        private static List<ulong> GetSuperUsers()
        {
            if (!File.Exists(Path.Combine(AppPath, "Data", "superusers.json")))
            {
                File.WriteAllText(Path.Combine(AppPath, "Data", "superusers.json"), JsonConvert.SerializeObject(new List<ulong>(), Formatting.Indented));
            }

            return JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(Path.Combine(AppPath, "Data", "superusers.json")));
        }

        /// <summary>
        /// Returns the root directory of the application.
        /// </summary>
        public static readonly string AppPath = Directory.GetParent(new Uri(Assembly.GetEntryAssembly()?.Location).LocalPath).FullName;

        /// <summary>
        /// My implementation of a static random; as close to fully random as possible.
        /// </summary>
        public static Random Random => local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));

        [ThreadStatic] private static Random local;
    }
}
