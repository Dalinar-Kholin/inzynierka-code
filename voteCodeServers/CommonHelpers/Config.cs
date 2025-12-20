using System;
using System.IO;
using System.Text.Json;

namespace VoteCodeServers.Helpers
{
    public class AppSettings
    {
        public string Alphabet { get; set; } = "";
        public int NumberOfCandidates { get; set; } = 0;
        public int NumberOfVoters { get; set; } = 0;
        public int SafetyParameter { get; set; } = 0;
        public int NumberOfServers { get; set; } = 0;
    }

    public static class Config
    {
        public static AppSettings Load(string path = "../appsettings.json")
        {
            var configPath = ResolveConfigPath(path);
            var json = File.ReadAllText(configPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings == null)
                throw new InvalidOperationException("Failed to deserialize appsettings.json");
            return settings;
        }

        private static string ResolveConfigPath(string initialPath)
        {
            var tried = new List<string>();

            var provided = Path.GetFullPath(initialPath);
            tried.Add(provided);
            if (File.Exists(provided)) return provided;

            string? dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var vcCandidate = Path.GetFullPath(Path.Combine(dir, "voteCodeServers", "appsettings.json"));
                tried.Add(vcCandidate);
                if (File.Exists(vcCandidate)) return vcCandidate;

                var directCandidate = Path.GetFullPath(Path.Combine(dir, "appsettings.json"));
                tried.Add(directCandidate);
                if (File.Exists(directCandidate)) return directCandidate;

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new FileNotFoundException("Config file not found. Tried:\n" + string.Join("\n", tried));
        }
    }
}