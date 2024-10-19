using HarmonyLib;
using UnityEngine;
using Verse;

namespace SafeTemperature
{
    public class SafeTemperatureMod : Mod
    {
        public const string PACKAGE_ID = "safetemperature.1trickPwnyta";
        public const string PACKAGE_NAME = "Safe Temperature";

        public static SafeTemperatureSettings Settings;

        public SafeTemperatureMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony(PACKAGE_ID);
            harmony.PatchAll();

            Settings = GetSettings<SafeTemperatureSettings>();

            Log.Message($"[{PACKAGE_NAME}] Loaded.");
        }

        public override string SettingsCategory() => PACKAGE_NAME;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            SafeTemperatureSettings.DoSettingsWindowContents(inRect);
        }
    }
}
