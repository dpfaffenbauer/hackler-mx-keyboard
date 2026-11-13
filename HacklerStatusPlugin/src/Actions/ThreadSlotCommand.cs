namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.Collections.Generic;

    // "Hackler Thread 1..9": one key per open Hackler thread (own, co-steered or invited).
    // Shows the title + state; a press opens the thread in the web app and marks it as seen.
    public class ThreadSlotCommand : PluginDynamicCommand
    {
        // The key image already carries the title, so no caption on keys. An empty string makes
        // the service fall back to the action name; a zero-width space draws nothing.
        private const String InvisibleCaption = "​";

        private readonly Dictionary<String, String> _signatures = new();

        public ThreadSlotCommand()
            : base()
        {
            for (var i = 1; i <= ThreadStore.SlotCount; i++)
            {
                this.AddParameter(ParameterFor(i), Strings.SlotName(i), Strings.Group);
            }

            ThreadStore.Instance.Updated += this.OnThreadsUpdated;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (ThreadStore.Instance.GetSlot(IndexOf(actionParameter)) is { } view)
            {
                ThreadStore.Instance.Open(view);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) =>
            KeyRenderer.RenderSlot(imageSize, ThreadStore.Instance.GetSlot(IndexOf(actionParameter)), ThreadStore.Instance.Connection);

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            imageSize == PluginImageSize.None ? Strings.SlotName(IndexOf(actionParameter) + 1) : InvisibleCaption;

        private void OnThreadsUpdated()
        {
            var connection = ThreadStore.Instance.Connection;
            for (var i = 1; i <= ThreadStore.SlotCount; i++)
            {
                var parameter = ParameterFor(i);
                var signature = KeyRenderer.SlotSignature(ThreadStore.Instance.GetSlot(i - 1), connection);
                if (!this._signatures.TryGetValue(parameter, out var previous) || previous != signature)
                {
                    this._signatures[parameter] = signature;
                    this.ActionImageChanged(parameter);
                }
            }
        }

        private static String ParameterFor(Int32 slotNumber) => $"slot{slotNumber}";

        private static Int32 IndexOf(String actionParameter) =>
            actionParameter != null && actionParameter.StartsWith("slot", StringComparison.Ordinal)
                && Int32.TryParse(actionParameter.AsSpan(4), out var n)
                ? n - 1
                : -1;
    }
}
