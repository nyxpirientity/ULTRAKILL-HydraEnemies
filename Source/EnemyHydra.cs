using System;
using Nyxpiri.Collections.Generic;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    public static class EnemyHydraEnemyComponentsExtension
    {
        public static EnemyHydra GetHydraComp(this EnemyComponents enemy)
        {
            return enemy.GetMonoByIndex<EnemyHydra>(EnemyHydra.MonoRegistrarIdx);
        }
    }

    public class EnemyHydra : MonoBehaviour
    {
        public class SharedData : ScriptableObject
        {
            protected SharedData()
            {
            }

            private ReserveList<GameObject> Instances = new ReserveList<GameObject>(16);
            public bool CountAsKill = false;
            public Bounds Bounds = new Bounds();
            public Action OnDeactivated = null;
            internal string CreatorName = "";
            public ScriptableObject EnemySpecificShared = null;
            public int InstanceCount { get => Instances.Count; }
            public int GlobalIdx { get; private set; } = -1;
            public bool Active { get; private set; } = false;

            internal void UnregisterInstance(int sharedIdx)
            {
                Instances.RemoveAt(sharedIdx);

                if (InstanceCount == 0)
                {
                    Deactivate();
                }
            }

            internal int RegisterInstance(GameObject gameObject)
            {
                int idx = Instances.Add(gameObject);

                if (!Active)
                {
                    Activate();
                }

                return idx;
            }

            private void Deactivate()
            {
                Assert.IsTrue(Active);

                Log.Debug($"EnemyHydraMod.SharedData '{name}' with creator '{CreatorName}' deactivated!");

                OnDeactivated?.Invoke();

                Active = false;
            }

            private void Activate()
            {
                Assert.IsFalse(Active);

                Log.Debug($"EnemyHydraMod.SharedData '{name}' with creator '{CreatorName}' activated!");

                Active = true;
            }

            private static int SharedIDIncrementer = 0;
            private void Awake()
            {
                name = name + SharedIDIncrementer.ToString();
                SharedIDIncrementer += 1;

                Log.Debug($"EnemyHydraMod.SharedData '{name}' with creator '{CreatorName}' awakened!");
            }

            private void OnDestroy()
            {
                Log.Debug($"EnemyHydraMod.SharedData '{name}' with creator '{CreatorName}' destroyed!");

                if (Active)
                {
                    Deactivate();
                }
            }
        }

        public bool CanDuplicate
        {
            get
            {
                return Shared.InstanceCount < Options.HydraMaxFromOne.Value && (NoDupeTime < 0.0f || Depth == 0);
            }
        }

        public SharedData Shared = null;
        public int Depth = -1;

        [NonSerialized] public EnemyIdentifier Eid = null;
        [NonSerialized] public EnemyComponents Enemy = null;

        public EnemyGameplayRank GameplayRank = EnemyGameplayRank.Ultraboss;
        [NonSerialized] public bool ContributesToInstanceCount = false;
        [NonSerialized] public Action PreDeath = null;

        public bool HydraKilled { get; private set; } = false;
        public bool HydraDuped { get; private set; } = false;
        public int SharedIdx { get; private set; } = -1;
        public string SharedName { get; private set; } = null;
        public static int MonoRegistrarIdx { get; private set; }

        private bool ExcludedFromHydraCheat = false;

        private float NoDupeTime = 0.0f;
        private bool NotifiedOfDeathCalled = false;

        protected void OnDestroy()
        {
            TryUnregisterWithShared();
        }

        protected void OnEnable()
        {
            if (SharedName == null)
            {
                return;
            }

            TryRegisterWithShared();
        }

        protected void OnDisable()
        {
            if (SharedName == null)
            {
                return;
            }

            TryUnregisterWithShared();
        }

        private void TryUnregisterWithShared()
        {
            if (ExcludedFromHydraCheat)
            {
                return;
            }

            Assert.IsNotNull(Shared, $"Shared was null! Shared Name: '{SharedName}'");

            if (ContributesToInstanceCount)
            {
                Log.Debug($"{name}: unregistered with shared!");

                if (!Eid.dead)
                {
                    PreDeath?.Invoke(); // ?????
                }

                Shared.UnregisterInstance(SharedIdx);
                SharedIdx = -1;
                ContributesToInstanceCount = false;

                if (Depth != 0)
                {
                    //Destroy(gameObject);
                }

                /*if (Shared.InstanceCount == 0)
                {
                    Destroy(Shared);
                }*/

                MusicManager.Instance?.PlayCleanMusic();
            }
        }

        protected void Update()
        {
            if (NyxLib.Cheats.IsCheatDisabled(Cheats.HydraMode))
            {
                return;
            }

            if (ExcludedFromHydraCheat)
            {
                return;
            }

            if (Eid.Dead)
            {
                return;
            }

            if (NoDupeTime >= 0.0f)
            {
                NoDupeTime -= Time.deltaTime / Mathf.Max(1.0f, (Shared.InstanceCount * 0.3f) + 0.667f);

                if (NoDupeTime <= 0.0f)
                {
                    NoDupeTime = -1.0f;
                }
            }
        }

        protected void Awake()
        {
            Eid = GetComponent<EnemyIdentifier>();
            Enemy = GetComponent<EnemyComponents>();

            if (Shared == null)
            {
                InitializeAsNew();
            }
        }

        protected void Start()
        {
            if (Eid.enemyType == EnemyType.Deathcatcher || Eid.enemyType == EnemyType.Idol || (Eid.enemyType == EnemyType.Centaur && Eid.gameObject.name.Contains("rain", StringComparison.OrdinalIgnoreCase)) || Eid.enemyType == EnemyType.V2Second || Enemy.UniquelySolo)
            {
                ExcludedFromHydraCheat = true;
                return;
            }

            Assert.IsNotNull(Eid, $"For object by name {gameObject.name}");

            if (Eid.dead)
            {
                return;
            }


            Assert.IsTrue(Depth >= 0, $"For object by name {gameObject.name}");
            Assert.IsNotNull(Shared, $"For object by name {gameObject.name} shared was null! Shared Name: '{SharedName}'");

            MusicManager.Instance?.PlayBattleMusic();

            if (Depth > 0)
            {
                Eid.dontCountAsKills = true;
            }
            else
            {
                Shared.CountAsKill = !Eid.dontCountAsKills;
            }

            GameplayRank = EnemyUtils.GetEnemyGameplayRank(Eid);

            if (Eid.enemyType == EnemyType.Providence)
            {
                NoDupeTime = Options.HydraBossWaitTime.Value;
            }
            else if (Eid.enemyType == EnemyType.MirrorReaper)
            {
                NoDupeTime = Eid.isBoss ? Options.HydraUltraBossWaitTime.Value : Options.HydraBossWaitTime.Value;
            }
            else
            {
                switch (GameplayRank)
                {
                    case EnemyGameplayRank.Normal:
                        NoDupeTime = Options.HydraDefaultWaitTime.Value;
                        break;
                    case EnemyGameplayRank.Miniboss:
                        NoDupeTime = Options.HydraMiniBossWaitTime.Value;
                        break;
                    case EnemyGameplayRank.Boss:
                        NoDupeTime = Options.HydraBossWaitTime.Value;
                        break;
                    case EnemyGameplayRank.Ultraboss:
                        NoDupeTime = Options.HydraUltraBossWaitTime.Value;
                        break;
                }
            }

            if (NyxLib.Cheats.Enabled)
            {
                var newHealth = Enemy.Health;
                for (int i = 0; i < Depth; i++)
                {
                    newHealth *= Options.HydraHealthDecayScale.Value;
                }

                Enemy.Health = newHealth;
            }

            if (Depth > 0)
            {
                if (Eid.enemyType == EnemyType.MaliciousFace)
                {
                    gameObject.transform.parent.gameObject.AddComponent<DestroyOnCheckpointRestart>();
                }
                else
                {
                    gameObject.AddComponent<DestroyOnCheckpointRestart>();
                }
            }

            DroneFlesh droneFlesh = Eid.GetComponent<DroneFlesh>();
            if (Depth > 0 && (droneFlesh != null))
            {
                var mainLight = GetComponent<Light>();
                if (mainLight != null)
                {
                    mainLight.enabled = false;
                }
                var lights = GetComponentsInChildren<Light>();
                foreach (var light in lights)
                {
                    light.enabled = false;
                }

            }

            if (droneFlesh != null)
            {
                gameObject.AddComponent<FleshDroneHydra>();
            }

            switch (Eid.enemyType)
            {
                case EnemyType.FleshPanopticon:
                    gameObject.AddComponent<FleshPanopticonHydra>();
                    break;
                case EnemyType.Drone:

                    break;

                default:
                    break;
            }

            TryRegisterWithShared();
        }

        private void TryRegisterWithShared()
        {
            Assert.IsNotNull(Shared, $"Shared was null! Shared Name: '{SharedName}'");

            if (!ContributesToInstanceCount)
            {
                Log.Debug($"{name}: registered with shared!");
                SharedIdx = Shared.RegisterInstance(gameObject);
                ContributesToInstanceCount = true;
            }
        }

        private static FieldAccess<TimeController, GameObject> parryLightFA = new FieldAccess<TimeController, GameObject>("parryLight");
        public void NotifyOfDeath(bool instakill)
        {
            if (NotifiedOfDeathCalled)
            {
                return;
            }

            Log.Debug($"{name}: EnemyHydraMod::NotifyOfDeath called with instakill as {instakill}");

            NotifiedOfDeathCalled = true;

            if (ExcludedFromHydraCheat)
            {
                return;
            }

            if (Eid == null)
            {
                Eid = GetComponent<EnemyIdentifier>();
            }

            if (Eid.Dead)
            {
                return;
            }

            if (!ContributesToInstanceCount)
            {
                return;
            }

            if (!NyxLib.Cheats.IsCheatEnabled(Cheats.HydraMode))
            {
                return;
            }

            Eid.dontCountAsKills = true;
            PreDeath?.Invoke();

            if (Depth == 0 && Shared.CountAsKill && !Cybergrind.IsActive)
            {
                // ContributeToActivateNextWave();
            }

            if (!instakill)
            {
                TryEnqueueDupe(false);
                TryEnqueueDupe(true);
            }

            TryUnregisterWithShared();

            if (!HydraDuped && Shared.InstanceCount == 0)
            {
                Log.Debug($"{name}: EnemyHydraMod::NotifyOfDeath called and we were hydrakilled, {Shared.InstanceCount} remaining instances, HydraDuped: {HydraDuped}");
                HydraKilled = true;

                if (Shared.CountAsKill)
                {
                    StatsManager.Instance.kills += 1;
                    ContributeToActivateNextWave();

                    if (Shared.CountAsKill && Cybergrind.IsActive)
                    {
                        //ContributeToActivateNextWave();
                    }
                }

                GameObject parrySound = UnityEngine.Object.Instantiate(parryLightFA.GetValue(TimeController.Instance), PlayerTracker.Instance.GetTarget().position, Quaternion.identity, PlayerTracker.Instance.GetTarget());

                if (parrySound.TryGetComponent<Light>(out var light))
                {
                    light.enabled = false;
                }

                Log.Debug($"{name}: About to try give points for hydra kill...");
                switch (GameplayRank)
                {
                    case EnemyGameplayRank.Normal:
                        StyleHUD.Instance.AddPoints(Options.HydraKillBonus.Value, "<color=#a2beff>HYDRA KILL</color>", null, Eid);
                        break;
                    case EnemyGameplayRank.Miniboss:
                        StyleHUD.Instance.AddPoints(Options.HydraMiniBossKillBonus.Value, "<color=#8d96fe>KINDA BIG HYDRA KILL</color>", null, Eid);
                        break;
                    case EnemyGameplayRank.Boss:
                        StyleHUD.Instance.AddPoints(Options.HydraBossKillBonus.Value, "<color=#8a2af7>BIG HYDRA KILL</color>", null, Eid);
                        break;
                    case EnemyGameplayRank.Ultraboss:
                        StyleHUD.Instance.AddPoints(0, "<color=#ffdb00>HOW??</color>", null, Eid);
                        StyleHUD.Instance.AddPoints(Options.HydraUltraBossKillBonus.Value, "<color=#ffdb00>ULTRA HYDRA KILL</color>", null, Eid);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                Log.Debug($"{name}: Points should have been given for hydra kill");
            }
            else
            {
                Eid.puppet = !instakill;
                if (Eid.enemyType == EnemyType.Providence)
                {
                    Eid.drone.spawnOnDeath = null;
                    Eid.drone.cantInstaExplode = true;
                }
                Log.Debug($"{name}: EnemyHydraMod::NotifyOfDeath called and we were NOT hydrakilled, {Shared.InstanceCount} remaining instances, HydraDuped: {HydraDuped}");
            }
        }

        private void ContributeToActivateNextWave()
        {
            Enemy.PrefabStore.ActivateNextWave?.AddDeadEnemy();
        }

        private void TryEnqueueDupe(bool isB)
        {
            if (ExcludedFromHydraCheat)
            {
                return;
            }

            if (!CanDuplicate)
            {
                return;
            }

            /*Assert.IsNotNull(GetComponent<EnemyComponents>(), $"For object by name {gameObject.name}");
            Assert.IsNotNull(GetComponent<EnemyComponents>().PrefabMod, $"For object by name {gameObject.name}");
            Assert.IsNotNull(GetComponent<EnemyComponents>().PrefabMod.Prefab, $"For object by name {gameObject.name}");
            */
            Log.Debug($"{name}: Now HydraDuped by TryEnqueueDupe");
            HydraDuped = true;
            HydraDupeQueue.QueuedDupeInfo dupeInfo = new HydraDupeQueue.QueuedDupeInfo();

            if (Eid.enemyType == EnemyType.Drone)
            {
                dupeInfo.Position = Eid.drone.GetComponent<Rigidbody>().transform.position;
            }
            else
            {
                dupeInfo.Position = transform.position;
            }

            dupeInfo.Rotation = transform.rotation;
            dupeInfo.LocalScale = transform.localScale;
            dupeInfo.SharedData = Shared;
            dupeInfo.Depth = Depth + 1;
            dupeInfo.EnemyType = Eid.enemyType;
            dupeInfo.BossBar = GetComponent<BossHealthBar>() != null;
            dupeInfo.InstanceStore = Enemy.PrefabStore.Instances;

            if (Eid.enemyType == EnemyType.Sisyphus)
            {
                dupeInfo.Position += (dupeInfo.Rotation * Vector3.right) * (isB ? -4.25f : 4.25f);
            }
            else
            {
                float additionalOffsetScalar = 1.0f;

                switch (Eid.enemyType)
                {
                    case EnemyType.HideousMass:
                        additionalOffsetScalar = 0.0f;
                        break;
                    case EnemyType.Minos:
                        additionalOffsetScalar = 0.0f;
                        break;
                    case EnemyType.FleshPanopticon:
                    case EnemyType.FleshPrison:
                        additionalOffsetScalar = 0.0f;
                        break;
                    case EnemyType.CancerousRodent:
                        additionalOffsetScalar = 0.00f;
                        break;
                    case EnemyType.VeryCancerousRodent:
                        additionalOffsetScalar = 0.00f;
                        break;
                    default:
                        break;
                }

                if (GetComponent<CancerousRodent>() != null)
                {
                    additionalOffsetScalar = 0.01f;
                }

                //dupeInfo.Position += (dupeInfo.Rotation * Vector3.Normalize(Vector3.Lerp(Vector3.right, Vector3.forward, UnityEngine.Random.Range(0.0f, 1.0f))))
                //                     * (isB ? -1.0f : 1.0f);
                dupeInfo.Position += Vector3.Project(dupeInfo.SharedData.Bounds.size, dupeInfo.Rotation * Vector3.right) * (isB ? -1.0f : 1.0f) * 0.3f * additionalOffsetScalar;
            }

            HydraDupeQueue.EnqueueDupe(dupeInfo);
        }

        private void InitializeAsNew()
        {
            Shared = ScriptableObject.CreateInstance<SharedData>();
            TryRegisterWithShared();
            Shared.Bounds = EnemyUtils.SolveEnemyBounds(gameObject);

            if (GetComponent<DroneFlesh>() != null)
            {
                Shared.EnemySpecificShared = new FleshDroneHydra.SharedData();
            }

            switch (Eid.enemyType)
            {
                case EnemyType.FleshPanopticon:
                    Shared.EnemySpecificShared = new FleshPanopticonHydra.SharedData();
                    break;
                default:
                    break;
            }

            Depth = 0;
            Shared.CreatorName = gameObject.name;
            SharedName = Shared.name;
        }

        internal void DuringDeath()
        {
            TryUnregisterWithShared();
        }

        internal static void Initialize()
        {
            MonoRegistrarIdx = EnemyComponents.MonoRegistrar.Register<EnemyHydra>();
        }
    }
}