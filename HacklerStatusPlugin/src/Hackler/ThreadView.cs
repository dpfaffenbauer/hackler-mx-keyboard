namespace Loupedeck.HacklerStatusPlugin
{
    using System;

    public enum ThreadDisplayState
    {
        Idle,       // turn finished and already seen
        Paused,     // stopped, daemon offline, budget
        Starting,   // queued or starting
        Working,    // working or landing
        Waiting,    // needs you: an approval or a question
        Done,       // turn finished, not seen yet
        Error,      // turn failed, not seen yet
        PrReady,    // PR opened, waiting for review
    }

    // A thread as it should be shown right now.
    public sealed class ThreadView
    {
        public ThreadView(HacklerThread thread, ThreadDisplayState state)
        {
            this.Thread = thread;
            this.State = state;
        }

        public HacklerThread Thread { get; }

        public ThreadDisplayState State { get; }

        // Since when the thread is in this state (ms), 0 = unknown.
        public Int64 Since => this.State switch
        {
            ThreadDisplayState.Waiting => this.Thread.AttentionSince,
            ThreadDisplayState.Working or ThreadDisplayState.Starting => this.Thread.ActivitySince > 0 ? this.Thread.ActivitySince : this.Thread.UpdatedAt,
            ThreadDisplayState.Done or ThreadDisplayState.Error => this.Thread.NewsAt,
            _ => this.Thread.UpdatedAt,
        };

        // Lower = more important; decides who gets a key when there are more threads than keys.
        public Int32 Priority => this.State switch
        {
            ThreadDisplayState.Waiting => 0,
            ThreadDisplayState.Error => 1,
            ThreadDisplayState.Done => 2,
            ThreadDisplayState.Working or ThreadDisplayState.Starting => 3,
            ThreadDisplayState.PrReady => 4,
            ThreadDisplayState.Idle => 5,
            _ => 6,
        };

        // `acknowledgedAt`: when this plugin last opened the thread (bridges the time until the
        // server's seen mark comes back with the next poll).
        public static ThreadDisplayState Map(HacklerThread t, Int64 acknowledgedAt)
        {
            var unread = t.Unread && t.NewsAt > acknowledgedAt;
            return t.Status switch
            {
                "queued" or "starting" => ThreadDisplayState.Starting,
                "working" or "landing" => ThreadDisplayState.Working,
                "pr_ready" => ThreadDisplayState.PrReady,
                "paused" => ThreadDisplayState.Paused,
                "waiting" when t.AttentionKind != null => ThreadDisplayState.Waiting,
                "waiting" when unread && t.OutcomeKind == "failed" => ThreadDisplayState.Error,
                "waiting" when unread => ThreadDisplayState.Done,
                _ => ThreadDisplayState.Idle,
            };
        }
    }
}
