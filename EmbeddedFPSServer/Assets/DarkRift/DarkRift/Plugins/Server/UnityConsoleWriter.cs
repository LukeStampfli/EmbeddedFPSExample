using UnityEngine;
using System.Collections.Generic;
using System;

namespace DarkRift.Server.Unity
{
    public sealed class UnityConsoleWriter : LogWriter
    {
        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public UnityConsoleWriter(LogWriterLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override void WriteEvent(WriteEventArgs args)
        {
            switch (args.LogType)
            {
                case LogType.Trace:
                case LogType.Info:
                    Debug.Log(args.FormattedMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(args.FormattedMessage);
                    break;
                case LogType.Error:
                case LogType.Fatal:
                    Debug.LogError(args.FormattedMessage);
                    break;
            }
        }
    }
}
