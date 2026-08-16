using UnityEngine;
using BepInEx;
using System;
using System.IO;
using Nyxpiri.ULTRAKILL.NyxLib;
using HarmonyLib;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    public static class Cheats
    {
        public const string HydraMode = "nyxpiri.hydra-mode";
    }

    [BepInPlugin("nyxpiri.ultrakill.hydra", "Hydra", "0.0.0")]
    [BepInProcess("ULTRAKILL.exe")]
    public class Hydra : BaseUnityPlugin
    {
        protected void Awake()
        {
            Log.Initialize(Logger);

            NyxLib.Cheats.ReadyForCheatRegistration += RegisterCheats;

            HydraDupeQueue.Initialize();
            EnemyHydra.Initialize();

            Options.Config = Config;
            Options.Initialize();

            Harmony.CreateAndPatchAll(GetType().Assembly);

            if (!File.Exists(Config.ConfigFilePath))
            {
                Config.Save();
            }
        }

        protected void Start()
        {
            NyxLib.LevelModder.RegisterLevelMod<P2Additions>("Level P-2");
        }

        protected void Update()
        {

        }

        protected void LateUpdate()
        {

        }

        protected void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Config.Reload();
            }
        }

        private void RegisterCheats(CheatsManager cheatsManager)
        {
            cheatsManager.RegisterCheat(new ToggleCheat(
                "Hydra Mode",
                Cheats.HydraMode,
                onDisable: (cheat) =>
                {
                },
                onEnable: (cheat, manager) =>
                {
                    EnemyCloneManager.RequestInstanceStoreCapacity(5);
                }
            ), "MITOSIS");

            if (NyxLib.Cheats.IsCheatEnabled(Cheats.HydraMode))
            {
                EnemyCloneManager.RequestInstanceStoreCapacity(5);
            }
        }
    }
}
