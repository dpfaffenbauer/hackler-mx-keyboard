namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.Linq;

    // Summary key: how many threads wait for you (approval / question); a press opens the one
    // waiting the longest - or, if none waits, the oldest unseen result.
    public class NextWaitingCommand : PluginDynamicCommand
    {
        private String _signature;

        public NextWaitingCommand()
            : base(displayName: Strings.NextWaitingName, description: Strings.NextWaitingDescription, groupName: Strings.Group)
        {
            ThreadStore.Instance.Updated += this.OnThreadsUpdated;
        }

        protected override void RunCommand(String actionParameter)
        {
            var all = ThreadStore.Instance.GetAll();
            var target = all.Where(v => v.State == ThreadDisplayState.Waiting).OrderBy(v => v.Since).FirstOrDefault()
                ?? all.Where(v => v.State is ThreadDisplayState.Done or ThreadDisplayState.Error).OrderBy(v => v.Since).FirstOrDefault();

            if (target != null)
            {
                ThreadStore.Instance.Open(target);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var (waiting, unread, working) = Count();
            return KeyRenderer.RenderSummary(imageSize, waiting, unread, working, ThreadStore.Instance.Connection);
        }

        private void OnThreadsUpdated()
        {
            var (waiting, unread, working) = Count();
            var signature = KeyRenderer.SummarySignature(waiting, unread, working, ThreadStore.Instance.Connection);
            if (signature != this._signature)
            {
                this._signature = signature;
                this.ActionImageChanged();
            }
        }

        private static (Int32 Waiting, Int32 Unread, Int32 Working) Count()
        {
            var all = ThreadStore.Instance.GetAll();
            return (
                all.Count(v => v.State == ThreadDisplayState.Waiting),
                all.Count(v => v.State is ThreadDisplayState.Done or ThreadDisplayState.Error),
                all.Count(v => v.State is ThreadDisplayState.Working or ThreadDisplayState.Starting));
        }
    }
}
