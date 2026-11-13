# Hackler threads on the Logitech MX Creative Keypad

A Logi Options+ plugin that puts your [Hackler](https://app.hackler.app) threads on the LCD keys of the
**MX Creative Keypad**: what each agent is doing and which thread needs you. One press opens the thread.

It shows the threads `GET /v1/threads` returns for your login: threads you created, threads you were invited
to (members) and threads you co-steered. Closed and archived threads are left out. Snoozed threads are left
out too, unless they wait for you.

## What the keys show

Each **Hackler Thread 1–9** key shows one thread: its title, its state, and how long it has been in that state.

| State | Key |
|---|---|
| **läuft / startet**: the agent works | dark key, status in Hackler orange, a bar sweeping along the bottom |
| **Freigabe / Frage**: an approval or a question waits for you | the whole key turns amber and pulses |
| **fertig**: the turn finished and you have not looked yet | green |
| **Fehler**: the turn failed and you have not looked yet | red |
| **PR bereit**: a PR is open | violet |
| **bereit**: finished and already seen | grey |
| **pausiert**: stopped, daemon offline, budget | dim grey |
| no thread | black |

"Seen" is Hackler's own seen mark, so a thread you opened on the iPhone or in the web app is grey on the
keypad too.

A press opens the thread in the web app (`<webUrl>/threads/<id>`) and marks it as seen on every device.
**Next waiting thread** shows how many threads wait for you and opens the one waiting longest. If none
waits, it opens the oldest unseen result.

A thread keeps its key while it is open. When all nine keys are taken, a thread that is working or needs you
replaces the least important thread on the keys, picking the one untouched the longest.

Texts are German when Logi Options+ runs in German, English otherwise.

## Setup

1. Sign in with the Hackler CLI once (the plugin reuses that login, it stores nothing itself):

   ```bash
   hackler login --server https://api.hackler.app --web https://app.hackler.app
   ```

   The login lives in `~/.config/hackler/cli.json` (`HACKLER_CLI_CONFIG` / `XDG_CONFIG_HOME` honoured).
   The plugin picks up a new login without a restart.
2. Install the plugin: double-click `HacklerStatus_x_y.lplug4` from the releases, or build it (below).
3. In Logi Options+, open the MX Creative Keypad, go to *All actions → Installed plugins → Hackler Threads*
   and drag **Hackler Thread 1–9** and **Next waiting thread** onto keys.

The plugin polls the API every 4 seconds and backs off up to a minute when the API cannot be reached.
A missing or expired login and an unreachable API show up as the plugin status in Options+. While the API
cannot be reached, the keys keep the last known state and say "offline".

## Development

Requirements: .NET 10 SDK, Logi Options+ with the Logi Plugin Service.

```bash
dotnet build HacklerStatusPlugin/HacklerStatusPlugin.slnx
```

A Debug build links the output folder into the Logi Plugin Service
(`~/Library/Application Support/Logi/LogiPluginService/Plugins/HacklerStatusPlugin.link`) and reloads the
plugin. Logs: `~/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/HacklerStatus.log`.

Package:

```bash
dotnet tool install --global LogiPluginTool
dotnet build HacklerStatusPlugin/HacklerStatusPlugin.slnx -c Release
logiplugintool pack HacklerStatusPlugin/bin/Release/ dist/HacklerStatus_1_0.lplug4
logiplugintool verify dist/HacklerStatus_1_0.lplug4
```

| File | Role |
|---|---|
| `Hackler/HacklerCredentials.cs` | reads the CLI login |
| `Hackler/HacklerClient.cs` | `GET /v1/threads`, `PUT /v1/threads/:id/marks` |
| `Hackler/ThreadView.cs` | thread status + attention + seen mark → key state |
| `Hackler/ThreadStore.cs` | polling, key assignment |
| `KeyRenderer.cs` | draws the keys |
| `Actions/*` | the two Options+ actions |
