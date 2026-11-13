namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.Text.Json;

    // The fields of a ThreadSummary (GET /v1/threads, packages/protocol/src/http.ts
    // in the hackler repo) the keys need. All times are ms since epoch.
    public sealed class HacklerThread
    {
        public String ThreadId { get; init; }

        public String Title { get; init; }

        // queued | starting | working | waiting | paused | pr_ready | landing | done
        public String Status { get; init; }

        public Int64 UpdatedAt { get; init; }

        // what a waiting thread waits for: "approval" | "question"; null = the turn ended
        public String AttentionKind { get; init; }

        public Int64 AttentionSince { get; init; }

        public Int64 ActivitySince { get; init; }

        // how the last turn ended: "pr" | "answer" | "failed" | "stopped"
        public String OutcomeKind { get; init; }

        public Int64 NewsAt { get; init; }

        public Int64 SeenAt { get; init; }

        public Int64 SnoozedUntil { get; init; }

        public String Repo { get; init; }

        public Boolean NeedsYou => this.Status == "waiting" && this.AttentionKind != null;

        // Something happened since the caller last looked at the thread (on any device).
        public Boolean Unread => this.NewsAt > 0 && this.NewsAt > this.SeenAt;

        public String DisplayName => String.IsNullOrWhiteSpace(this.Title) ? (this.Repo ?? "Thread") : this.Title;

        public static HacklerThread Parse(JsonElement t)
        {
            var attention = Obj(t, "attention");
            var activity = Obj(t, "activity");
            var outcome = Obj(t, "outcome");
            var news = Obj(t, "news");
            var marks = Obj(t, "marks");
            var repo = Str(t, "repo");
            return new HacklerThread
            {
                ThreadId = Str(t, "threadId"),
                Title = Str(t, "title"),
                Status = Str(t, "status") ?? "",
                UpdatedAt = Num(t, "updatedAt"),
                AttentionKind = attention is { } a ? Str(a, "kind") ?? "approval" : null,
                AttentionSince = attention is { } a2 ? Num(a2, "since") : 0,
                ActivitySince = activity is { } ac ? Num(ac, "since") : 0,
                OutcomeKind = outcome is { } o ? Str(o, "kind") : null,
                NewsAt = news is { } n ? Num(n, "at") : 0,
                SeenAt = marks is { } m ? Num(m, "seenAt") : 0,
                SnoozedUntil = marks is { } m2 ? Num(m2, "snoozedUntil") : 0,
                Repo = repo == null ? null : System.IO.Path.GetFileName(repo.TrimEnd('/')),
            };
        }

        private static JsonElement? Obj(JsonElement e, String name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Object ? v : null;

        private static String Str(JsonElement e, String name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static Int64 Num(JsonElement e, String name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) ? n : 0;
    }
}
