using UnityEngine;
using BepInEx;
using System;
using System.IO;
using Nyxpiri.ULTRAKILL.NyxLib;
using HarmonyLib;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    [HarmonyPatch(typeof(SisyphusPrimeIntro), "OnCollisionEnter")]
    static class SisyphusPrimeIntroOnCollisionEnterPatch
    {
        public static void Prefix(SisyphusPrimeIntro __instance, Collider col)
        {
        }

        public static void Postfix(SisyphusPrimeIntro __instance, Collider col)
        {
            var hasHitGround = new FieldPublisher<SisyphusPrimeIntro, bool>(__instance, "hasHitGround");
            if (hasHitGround.Value)
            {
                if (NyxLib.Cheats.IsCheatEnabled(Cheats.HydraMode))
                {
                    __instance.gameObject.GetComponent<Collider>().enabled = false;
                    __instance.GetComponent<Rigidbody>().detectCollisions = false;
                    var rbs = __instance.GetComponentsInChildren<Rigidbody>();
                    var cols = __instance.GetComponentsInChildren<Collider>();

                    foreach (var ccol in cols)
                    {
                        ccol.enabled = false;
                    }

                    foreach (var rb in rbs)
                    {
                        rb.detectCollisions = false;
                    }
                }
            }
        }
    }

    public class P2Additions : LevelMod
    {
        public override void OnSceneLoad()
        {
            Log.Debug($"Level Additions for P-2 OnSceneLoad called!");
            EnemyEvents.PostStart += OnEnemySpawned;
            EnemyEvents.Death += OnEnemyDie;
            EnemyEvents.PreDestroy += OnEnemyDestroy;
            UpdateEvents.OnUpdate += Update;
        }

        private void Update()
        {
            createPanopticonRadioQueued -= 1;
            if (createPanopticonRadioQueued == 1 && PanopticonRadio == null)
            {
                createPanopticonRadioQueued = 0;
                var bossMusics = GameObject.Find("BossMusics");
                var audioSources = bossMusics.GetComponentsInChildren<AudioSource>();

                foreach (var audioSource in audioSources)
                {
                    if (audioSource.clip == null)
                    {
                        continue;
                    }

                    if (audioSource.clip.name.Contains("panopticon"))
                    {
                        PanopticonRadio = GameObject.Instantiate(audioSource.gameObject);
                        PanopticonRadio.SetActive(false);
                        audioSource.maxDistance = 400.0f;
                    }
                }
            }
        }

        public override void OnSceneUnload()
        {
            EnemyEvents.PostStart -= OnEnemySpawned;
            EnemyEvents.Death -= OnEnemyDie;
            EnemyEvents.PreDestroy -= OnEnemyDestroy;
            UpdateEvents.OnUpdate -= Update;

            DisableBattleWithClean();
        }

        private int NumVirtues = 0;
        private int NumMindflayers = 0;
        private int NumHideousMasses = 0;
        private bool IsBattleWithClean = false;

        private GameObject PanopticonRadio = null;

        private void OnEnemySpawned(EnemyComponents enemy)
        {
            Assert.IsTrue(enemy.gameObject.activeInHierarchy);

            if (enemy.Eid.Dead)
            {
                return;
            }

            if (!enemy.enabled || !enemy.isActiveAndEnabled)
            {
                return;
            }

            switch (enemy.Eid.enemyType)
            {
                case global::EnemyType.Mindflayer:
                    Log.Debug($"P2Additions Detected a Mindflayer spawn!");
                    NumMindflayers += 1;
                    break;
                case global::EnemyType.Virtue:
                    Log.Debug($"P2Additions Detected a Virtue spawn!");
                    NumVirtues += 1;
                    break;
                case global::EnemyType.HideousMass:
                    Log.Debug($"P2Additions Detected a HideousMass spawn!");
                    NumHideousMasses += 1;
                    break;
                default:
                    break;
            }

            if (NyxLib.Cheats.IsCheatEnabled(Cheats.HydraMode))
            {
                if (NumMindflayers >= 2 && NumVirtues >= 2)
                {
                    EnableBattleWithClean();
                }
                else if (NumVirtues >= 3 && NumHideousMasses >= 1)
                {
                    EnableBattleWithClean();
                }
            }
            else
            {
                DisableBattleWithClean();
            }

            if (enemy.Eid.enemyType == global::EnemyType.FleshPanopticon && NyxLib.Cheats.IsCheatEnabled(Cheats.HydraMode))
            {
                createPanopticonRadioQueued = PanopticonRadio == null ? 20 : -1;
            }
        }

        long createPanopticonRadioQueued = 0;

        private void EnableBattleWithClean()
        {
            if (!IsBattleWithClean)
            {
                Log.Debug($"Level Additions for P-2 trying to play battle music with clean!\nNumVirtues: {NumVirtues}\nNumMindflayers: {NumMindflayers}");
                Music.AddPlayCleanWithBattleVote();
                IsBattleWithClean = true;
            }
        }

        private void DisableBattleWithClean()
        {
            if (IsBattleWithClean)
            {
                Log.Debug($"Level Additions for P-2 trying to NOT play battle music with clean!");
                Music.SubtractPlayCleanWithBattleVote();
                IsBattleWithClean = false;
            }
        }

        private void OnEnemyDie(EnemyComponents enemy)
        {
            switch (enemy.Eid.enemyType)
            {
                case global::EnemyType.Mindflayer:
                    Log.Debug($"P2Additions Detected a Mindflayer death!");
                    NumMindflayers -= 1;
                    break;
                case global::EnemyType.Virtue:
                    Log.Debug($"P2Additions Detected a Virtue death!");
                    NumVirtues -= 1;
                    break;
                case global::EnemyType.HideousMass:
                    Log.Debug($"P2Additions Detected a HideousMass death!");
                    NumHideousMasses -= 1;
                    break;
                case global::EnemyType.FleshPanopticon:
                    if (NyxLib.Cheats.IsCheatEnabled(Cheats.HydraMode))
                    {
                        if (!PanopticonRadio.activeSelf)
                        {
                            enemy.GetComponent<EnemyHydra>().Shared.OnDeactivated += () =>
                            {
                                if (PanopticonRadio != null)
                                {
                                    GameObject.Destroy(PanopticonRadio.gameObject);
                                }
                            };
                            PanopticonRadio.GetComponent<AudioSource>().time = 22.0f;
                            PanopticonRadio.GetComponent<AudioSource>().volume += 0.1f;
                            PanopticonRadio.NullInvalid()?.SetActive(true);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnEnemyDestroy(EnemyComponents enemy)
        {
            if (enemy.Eid.dead)
            {
                return;
            }

            switch (enemy.Eid.enemyType)
            {
                case global::EnemyType.Mindflayer:
                    Log.Debug($"P2Additions Detected a Mindflayer destruction!");
                    NumMindflayers -= 1;
                    break;
                case global::EnemyType.Virtue:
                    Log.Debug($"P2Additions Detected a Virtue destruction!");
                    NumVirtues -= 1;
                    break;
                case global::EnemyType.HideousMass:
                    Log.Debug($"P2Additions Detected a HideousMass destruction!");
                    NumHideousMasses -= 1;
                    break;
                default:
                    break;
            }
        }
    }
}