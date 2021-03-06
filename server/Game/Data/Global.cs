#region

using System;
using Game.Util;

#endregion

namespace Game.Data
{
    public class Global : IGlobal
    {
        [Obsolete("Inject the property you need directly", false)]
        public static IGlobal Current { get; set; }

        public IChannel Channel { get; private set; }
        /// <summary>
        /// Matches an alphanumeric string that is at least 2 characters and does not begin or end with space.
        /// </summary>
        public const string ALPHANUMERIC_NAME = "^([a-z0-9][a-z0-9 ]*[a-z0-9])$";

        public Global(IChannel channel)
        {
            Channel = channel;
            FireEvents = true;
        }

        public bool FireEvents { get; set; }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}