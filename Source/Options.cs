
using BepInEx.Configuration;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    public static class Options
    {
        public static ConfigEntry<bool> LogDebugInfo = null;
        public static ConfigEntry<float> HydraHealthDecayScale = null;
        public static ConfigEntry<float> HydraDefaultWaitTime = null;
        public static ConfigEntry<float> HydraMiniBossWaitTime = null;
        public static ConfigEntry<float> HydraBossWaitTime = null;
        public static ConfigEntry<float> HydraUltraBossWaitTime = null;
        public static ConfigEntry<int> HydraMaxDepth = null;
        public static ConfigEntry<int> HydraMaxFromOneBoss = null;
        public static ConfigEntry<int> HydraMaxFromOne = null;
        public static ConfigEntry<int> HydraMaxPerUpdate = null;
        public static ConfigEntry<int> HydraPrefabPoolCapacity = null;
        public static ConfigEntry<int> HydraPrefabPoolGrowPerUpdate = null;
        public static ConfigEntry<int> HydraKillBonus = null;
        public static ConfigEntry<int> HydraMiniBossKillBonus = null;
        public static ConfigEntry<int> HydraBossKillBonus = null;
        public static ConfigEntry<int> HydraUltraBossKillBonus = null;

        public static void Initialize()
        {
            HydraHealthDecayScale = Config.Bind($"Balance", "HydraHealthDecayScale", 0.5f);

            HydraDefaultWaitTime = Config.Bind($"Balance.Normal", "HydraDefaultWaitTime", 0.45f);
            HydraMiniBossWaitTime = Config.Bind($"", "HydraMiniBossWaitTime", 0.75f);
            HydraBossWaitTime = Config.Bind($"", "HydraBossWaitTime", 1.5f);
            HydraUltraBossWaitTime = Config.Bind($"", "HydraUltraBossWaitTime", 2.75f);

            HydraMaxFromOneBoss = Config.Bind($"Balance.Boss", "HydraMaxFromOneBoss", 6);
            HydraMaxDepth = Config.Bind($"Balance", "HydraMaxDepth", 16);
            HydraMaxFromOne = Config.Bind($"Balance.Normal", "HydraMaxFromOne", 12);
            HydraMaxPerUpdate = Config.Bind($"Performance", "HydraMaxPerFrame", 4);

            HydraPrefabPoolCapacity = Config.Bind($"Performance", "HydraPrefabPoolCapacity", 20);
            HydraPrefabPoolGrowPerUpdate = Config.Bind($"Performance", "HydraPrefabPoolGrowPerUpdate", 3);

            HydraKillBonus = Config.Bind($"Style", "HydraKillBonus", 10);
            HydraMiniBossKillBonus = Config.Bind($"Style", "HydraMiniBossKillBonus", 50);
            HydraBossKillBonus = Config.Bind($"Style", "HydraBossKillBonus", 100);
            HydraUltraBossKillBonus = Config.Bind($"Style", "HydraUltraBossKillBonus", 1000);

            LogDebugInfo = Config.Bind("Diagnostics", "LogDebugInfo", false);
        }
        
        internal static ConfigFile Config = null;
    }
}