namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public enum ConnectionState
    {
        Ok,
        NoLogin,
        Unauthorized,
        Unreachable,
    }

    // Polls GET /v1/threads and keeps the thread -> key assignment.
    // A thread keeps its key while it is open; a thread that needs you takes the key of a
    // less important one when all keys are taken.
    public sealed class ThreadStore
    {
        public const Int32 SlotCount = 9;

        private const Int32 TickMs = 1000;
        private const Int32 PollMs = 4000;
        private const Int32 MaxBackoffMs = 60000;
        private const Int32 FailuresBeforeUnreachable = 3;

        private readonly Object _lock = new();
        private readonly String[] _slots = new String[SlotCount];
        private readonly Dictionary<String, ThreadView> _views = new();
        private readonly Dictionary<String, Int64> _acknowledged = new();

        private Timer _tick;
        private CancellationTokenSource _cts;
        private HacklerCredentials _credentials;
        private DateTime _credentialsWrite = DateTime.MinValue;
        private List<HacklerThread> _threads = new();
        private ConnectionState _connection = ConnectionState.Ok;
        private Int32 _failures;

        private ThreadStore()
        {
        }

        public static ThreadStore Instance { get; } = new ThreadStore();

        // Fired every second (animations) and after every poll.
        public event Action Updated;

        public event Action<ConnectionState> ConnectionChanged;

        public ConnectionState Connection
        {
            get
            {
                lock (this._lock)
                {
                    return this._connection;
                }
            }
        }

        public void Start()
        {
            this.Stop();
            this._cts = new CancellationTokenSource();
            var token = this._cts.Token;
            _ = Task.Run(() => this.PollLoop(token), token);
            this._tick = new Timer(_ => this.Updated?.Invoke(), null, TickMs, TickMs);
        }

        public void Stop()
        {
            this._cts?.Cancel();
            this._cts = null;
            this._tick?.Dispose();
            this._tick = null;
        }

        public ThreadView GetSlot(Int32 index)
        {
            lock (this._lock)
            {
                return index >= 0 && index < SlotCount && this._slots[index] is { } id && this._views.TryGetValue(id, out var view)
                    ? view
                    : null;
            }
        }

        public IReadOnlyList<ThreadView> GetAll()
        {
            lock (this._lock)
            {
                return this._views.Values.ToList();
            }
        }

        // Opens the thread in the web app and marks it as seen (locally right away, on the
        // server for every device).
        public void Open(ThreadView view)
        {
            HacklerCredentials creds;
            lock (this._lock)
            {
                creds = this._credentials;
                this._acknowledged[view.Thread.ThreadId] = Math.Max(view.Thread.NewsAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                this.Rebuild();
            }

            this.Updated?.Invoke();

            var url = $"{creds?.WebUrl ?? HacklerCredentials.DefaultWebUrl}/threads/{Uri.EscapeDataString(view.Thread.ThreadId)}";
            try
            {
                Process.Start(new ProcessStartInfo("open", url) { UseShellExecute = false })?.Dispose();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Could not open {url}");
            }

            if (creds != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HacklerClient.MarkSeenAsync(creds, view.Thread.ThreadId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Warning(ex, "Marking thread as seen failed");
                    }
                });
            }
        }

        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var delay = await this.PollOnce().ConfigureAwait(false);
                try
                {
                    await Task.Delay(delay, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        // Returns the delay until the next poll.
        private async Task<Int32> PollOnce()
        {
            // pick up a new `hackler login` without restarting the plugin
            var write = HacklerCredentials.LastWriteUtc;
            if (write != this._credentialsWrite || this._credentials == null)
            {
                this._credentialsWrite = write;
                this._credentials = HacklerCredentials.Load();
            }

            var creds = this._credentials;
            if (creds == null)
            {
                this.SetThreads(new List<HacklerThread>(), ConnectionState.NoLogin);
                return PollMs;
            }

            try
            {
                var threads = await HacklerClient.ListThreadsAsync(creds).ConfigureAwait(false);
                this._failures = 0;
                this.SetThreads(threads, ConnectionState.Ok);
                return PollMs;
            }
            catch (HacklerUnauthorizedException)
            {
                this.SetThreads(new List<HacklerThread>(), ConnectionState.Unauthorized);
                return PollMs * 4;
            }
            catch (Exception ex)
            {
                this._failures++;
                PluginLog.Warning($"Polling Hackler failed ({this._failures}x): {ex.Message}");
                if (this._failures >= FailuresBeforeUnreachable)
                {
                    // keep the last known threads on the keys
                    this.SetThreads(null, ConnectionState.Unreachable);
                }

                return Math.Min(MaxBackoffMs, PollMs * (1 << Math.Min(this._failures, 4)));
            }
        }

        private void SetThreads(List<HacklerThread> threads, ConnectionState connection)
        {
            Boolean connectionChanged;
            lock (this._lock)
            {
                if (threads != null)
                {
                    this._threads = threads;
                }

                connectionChanged = this._connection != connection;
                this._connection = connection;
                this.Rebuild();
            }

            if (connectionChanged)
            {
                this.ConnectionChanged?.Invoke(connection);
            }

            this.Updated?.Invoke();
        }

        // Caller holds the lock.
        private void Rebuild()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this._views.Clear();
            foreach (var t in this._threads)
            {
                if (t.Status == "done")
                {
                    continue;
                }

                this._acknowledged.TryGetValue(t.ThreadId, out var ackAt);
                var view = new ThreadView(t, ThreadView.Map(t, ackAt));

                // snoozed threads stay off the keys unless they need you
                if (t.SnoozedUntil > now && view.State != ThreadDisplayState.Waiting)
                {
                    continue;
                }

                this._views[t.ThreadId] = view;
            }

            foreach (var id in this._acknowledged.Keys.Where(id => !this._views.ContainsKey(id)).ToList())
            {
                this._acknowledged.Remove(id);
            }

            for (var i = 0; i < SlotCount; i++)
            {
                if (this._slots[i] != null && !this._views.ContainsKey(this._slots[i]))
                {
                    this._slots[i] = null;
                }
            }

            var unassigned = this._views.Values
                .Where(v => Array.IndexOf(this._slots, v.Thread.ThreadId) < 0)
                .OrderBy(v => v.Priority)
                .ThenByDescending(v => v.Thread.UpdatedAt);

            foreach (var view in unassigned)
            {
                var free = Array.IndexOf(this._slots, null);
                if (free >= 0)
                {
                    this._slots[free] = view.Thread.ThreadId;
                    continue;
                }

                // all keys taken: a thread that is working or needs you replaces the least
                // important, longest untouched one
                if (view.Priority > 3)
                {
                    break;
                }

                var victim = Enumerable.Range(0, SlotCount)
                    .Select(i => (Index: i, View: this._views[this._slots[i]]))
                    .Where(s => s.View.Priority > view.Priority)
                    .OrderByDescending(s => s.View.Priority)
                    .ThenBy(s => s.View.Thread.UpdatedAt)
                    .Select(s => (Int32?)s.Index)
                    .FirstOrDefault();

                if (victim is { } index)
                {
                    this._slots[index] = view.Thread.ThreadId;
                }
            }
        }
    }
}
