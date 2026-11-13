namespace Loupedeck.HacklerStatusPlugin
{
    using System;

    // Shows your Hackler threads on the MX Creative Keypad.
    // Data flow: hackler CLI login (~/.config/hackler/cli.json) -> GET /v1/threads every few
    //            seconds -> ThreadStore -> ThreadSlotCommand / NextWaitingCommand.
    public class HacklerStatusPlugin : Plugin
    {
        public const String GuideUrl = "https://app.hackler.app";

        public HacklerStatusPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
            this.InitStrings();
        }

        public override Boolean UsesApplicationApiOnly => true;

        public override Boolean HasNoApplication => true;

        public override void Load()
        {
            this.InitStrings();
            ThreadStore.Instance.ConnectionChanged += this.OnConnectionChanged;
            ThreadStore.Instance.Start();
        }

        public override void Unload()
        {
            ThreadStore.Instance.ConnectionChanged -= this.OnConnectionChanged;
            ThreadStore.Instance.Stop();
        }

        private void OnConnectionChanged(ConnectionState state)
        {
            PluginLog.Info($"Hackler connection: {state}");
            switch (state)
            {
                case ConnectionState.Ok:
                    this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, null);
                    break;
                case ConnectionState.NoLogin:
                    this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, Strings.NoLogin);
                    break;
                case ConnectionState.Unauthorized:
                    this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, Strings.Unauthorized);
                    break;
                default:
                    this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, Strings.Unreachable);
                    break;
            }
        }

        private void InitStrings()
        {
            try
            {
                Strings.Init(this.Localization?.LoupedeckLanguage);
            }
            catch (Exception)
            {
                // localization engine not ready yet - keep the system language
            }
        }
    }
}
