namespace Loupedeck.HacklerStatusPlugin
{
    using System;

    public sealed class HacklerUnauthorizedException : Exception
    {
        public HacklerUnauthorizedException()
            : base("401 Unauthorized")
        {
        }
    }
}
