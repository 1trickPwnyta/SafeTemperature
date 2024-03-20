namespace SafeTemperature
{
    public static class Debug
    {
        public static void Log(string message)
        {
#if DEBUG
            Verse.Log.Message($"[{SafeTemperatureMod.PACKAGE_NAME}] {message}");
#endif
        }
    }
}
