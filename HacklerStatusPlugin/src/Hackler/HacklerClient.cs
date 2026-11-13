namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    // The two calls the plugin makes against the Hackler API.
    public static class HacklerClient
    {
        // Cloudflare in front of the API rejects requests without a User-Agent of their own.
        private static readonly HttpClient Http = CreateClient();

        // GET /v1/threads: own threads, co-steered threads and threads the user is a member of
        // (archived ones are left out by the server).
        public static async Task<List<HacklerThread>> ListThreadsAsync(HacklerCredentials creds)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, creds.BaseUrl + "/v1/threads");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.Token);
            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HacklerUnauthorizedException();
            }

            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            var threads = new List<HacklerThread>();
            if (doc.RootElement.TryGetProperty("threads", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in list.EnumerateArray())
                {
                    var thread = HacklerThread.Parse(t);
                    if (!String.IsNullOrEmpty(thread.ThreadId))
                    {
                        threads.Add(thread);
                    }
                }
            }

            return threads;
        }

        // PUT /v1/threads/:id/marks: "seen" for all of the user's devices. seenAt/openedAt mean
        // "now" - the server stores its own clock.
        public static async Task MarkSeenAsync(HacklerCredentials creds, String threadId)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{creds.BaseUrl}/v1/threads/{Uri.EscapeDataString(threadId)}/marks")
            {
                Content = new StringContent($"{{\"seenAt\":{now},\"openedAt\":{now}}}", Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.Token);
            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                PluginLog.Warning($"Marking {threadId} as seen failed: {(Int32)response.StatusCode}");
            }
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("hackler-mx-keypad/1.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
