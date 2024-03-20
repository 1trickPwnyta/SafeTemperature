using HarmonyLib;
using Verse;

namespace SafeTemperature
{
    public class SafeTemperatureMod : Mod
    {
        public const string PACKAGE_ID = "safetemperature.1trickPwnyta";
        public const string PACKAGE_NAME = "Safe Temperature";

        public SafeTemperatureMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony(PACKAGE_ID);
            harmony.PatchAll();

            Log.Message($"[{PACKAGE_NAME}] Loaded.");
        }
    }
}
