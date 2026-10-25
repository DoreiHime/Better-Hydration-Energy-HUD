using System;
using UnityEngine;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>Small logging helper; keeps the log readable and quiet by default.</summary>
    public static class Log
    {
        private const string Prefix = "[BHE] ";
        public static bool Verbose;

        public static void Info(string message)
        {
            UnityEngine.Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            UnityEngine.Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(Prefix + message);
        }

        public static void Error(string message, Exception e)
        {
            UnityEngine.Debug.LogError(Prefix + message + "\n" + e);
        }

        public static void Debug(string message)
        {
            if (Verbose) UnityEngine.Debug.Log(Prefix + message);
        }
    }
}
