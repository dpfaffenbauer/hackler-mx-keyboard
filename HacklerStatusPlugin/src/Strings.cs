namespace Loupedeck.HacklerStatusPlugin
{
    using System;
    using System.Globalization;

    // Texts drawn on the keys and shown in Options+. English by default, German when
    // Options+ (or, as a fallback, macOS) runs in German.
    public static class Strings
    {
        private static Boolean _german = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";

        public static void Init(String language)
        {
            if (!String.IsNullOrEmpty(language))
            {
                _german = language.StartsWith("de", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static String Working => _german ? "läuft" : "working";

        public static String Starting => _german ? "startet" : "starting";

        public static String Approval => _german ? "Freigabe" : "approve";

        public static String Question => _german ? "Frage" : "question";

        public static String Done => _german ? "fertig" : "done";

        public static String Error => _german ? "Fehler" : "error";

        public static String PrReady => _german ? "PR bereit" : "PR ready";

        public static String Paused => _german ? "pausiert" : "paused";

        public static String Idle => _german ? "bereit" : "ready";

        public static String SummaryWaitingOne => _german ? "wartet" : "waiting";

        public static String SummaryWaitingMany => _german ? "warten" : "waiting";

        public static String SummaryNew => _german ? "neu" : "new";

        public static String SummaryWorking => _german ? "laufen" : "working";

        public static String SummaryQuiet => _german ? "ruhig" : "quiet";

        public static String Offline => "offline";

        public static String NoLoginShort => _german ? "Login fehlt" : "no login";

        public static String Group => "Hackler";

        public static String SlotName(Int32 number) => $"Hackler Thread {number}";

        public static String NextWaitingName => _german ? "Nächster wartender Thread" : "Next waiting thread";

        public static String NextWaitingDescription => _german
            ? "Zeigt, wie viele Hackler-Threads auf dich warten, und öffnet den, der am längsten wartet"
            : "Shows how many Hackler threads wait for you and opens the one waiting the longest";

        public static String NoLogin => _german
            ? "Nicht bei Hackler angemeldet – im Terminal `hackler login` ausführen."
            : "Not signed in to Hackler - run `hackler login` in a terminal.";

        public static String Unauthorized => _german
            ? "Der Hackler-Login ist abgelaufen – im Terminal `hackler login` erneut ausführen."
            : "The Hackler login has expired - run `hackler login` again in a terminal.";

        public static String Unreachable => _german
            ? "Hackler ist gerade nicht erreichbar – die Tasten zeigen den letzten bekannten Stand."
            : "Hackler cannot be reached right now - the keys show the last known state.";
    }
}
