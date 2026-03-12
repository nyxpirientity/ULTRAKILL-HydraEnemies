using UnityEngine;
using BepInEx;
using System;
using System.IO;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    public static class Cheats
    {
        public const string HydraMode = "nyxpiri.hydra-mode";
    }
    
    [BepInPlugin("com.nyxpiri.bepinex.plugins.ultrakill.hydra", "Hydra", "0.0.0.1")]
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

            if (!File.Exists(Config.ConfigFilePath))
            {
                Config.Save();
            }
        }

        protected void Start()
        {
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
                }
            ), "MITOSIS");
        }
    }
}
