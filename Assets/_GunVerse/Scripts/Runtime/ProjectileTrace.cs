using UnityEngine;

namespace GunVerse
{
    public static class ProjectileTrace
    {
        private const string PREFIX = "[ProjectileTrace]";

        public static bool Enabled { get; set; } = true;

        public static void Log(string message)
        {
            if (!Enabled)
            {
                return;
            }

            Debug.Log(PREFIX + " " + message);
        }
    }
}
