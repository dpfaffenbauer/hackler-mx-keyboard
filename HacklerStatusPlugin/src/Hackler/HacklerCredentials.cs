namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.IO;
    using System.Text.Json;

    // The login of the hackler CLI (`hackler login`), which the plugin reuses:
    //   ~/.config/hackler/cli.json  { baseUrl, token, login, userId?, webUrl? }
    // HACKLER_CLI_CONFIG and XDG_CONFIG_HOME are honoured like the CLI does
    // (apps/cli/src/config.ts in the hackler repo).
    public sealed class HacklerCredentials
    {
        public const String DefaultWebUrl = "https://app.hackler.app";

        private HacklerCredentials(String baseUrl, String token, String login, String webUrl)
        {
            this.BaseUrl = baseUrl;
            this.Token = token;
            this.Login = login;
            this.WebUrl = webUrl;
        }

        public String BaseUrl { get; }

        public String Token { get; }

        public String Login { get; }

        // Web app origin for thread links.
        public String WebUrl { get; }

        public static String Path
        {
            get
            {
                var explicitPath = Environment.GetEnvironmentVariable("HACKLER_CLI_CONFIG");
                if (!String.IsNullOrEmpty(explicitPath))
                {
                    return explicitPath;
                }

                var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (String.IsNullOrEmpty(configHome))
                {
                    configHome = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                }

                return System.IO.Path.Combine(configHome, "hackler", "cli.json");
            }
        }

        // null when there is no login (file missing or without server/token).
        public static HacklerCredentials Load()
        {
            var path = Path;
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                var baseUrl = Str(root, "baseUrl");
                var token = Str(root, "token");
                if (String.IsNullOrEmpty(baseUrl) || String.IsNullOrEmpty(token))
                {
                    return null;
                }

                var webUrl = Str(root, "webUrl");
                return new HacklerCredentials(
                    baseUrl.TrimEnd('/'),
                    token,
                    Str(root, "login") ?? "",
                    String.IsNullOrEmpty(webUrl) ? DefaultWebUrl : webUrl.TrimEnd('/'));
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, $"Could not read {path}");
                return null;
            }
        }

        public static DateTime LastWriteUtc => File.Exists(Path) ? File.GetLastWriteTimeUtc(Path) : DateTime.MinValue;

        private static String Str(JsonElement obj, String name) =>
            obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }
}
