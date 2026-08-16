using UnityEngine;
using System.Collections.Generic;
using Nyxpiri.ULTRAKILL.NyxLib;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    public static class HydraDupeQueue
    {
        public struct QueuedDupeInfo
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
            public EnemyHydra.SharedData SharedData;
            public EnemyCloneStore CloneStore;
            public int Depth;
            public EnemyType EnemyType;
            public Transform CloneParent;
            public bool BossBar;
        }

        public static void Initialize()
        {
            EnemyEvents.PreDeath += PreEnemyDeath;
            EnemyEvents.PreDeath += (eid, instakill) =>
            {
                var go = eid.gameObject;
                var enemy = go.GetComponent<EnemyComponents>();
                var ehm = enemy.GetHydraComp();

                ehm.NotifyOfDeath(instakill);
            };

            EnemyEvents.Death += DuringEnemyDeath;
            EnemyEvents.PostDeath += PostEnemyDeath;
            UpdateEvents.OnLateUpdate += LateUpdate;
            UpdateEvents.OnFixedUpdate += FixedUpdate;
        }

        public static int InstantiatedThisTick = 0;
        public static ulong NumFixedUpdates = 0;
        private static void FixedUpdate()
        {
            if (NyxLib.Cheats.IsCheatDisabled(Cheats.HydraMode))
            {
                return;
            }

            for (int i = InstantiatedThisTick; (i < Options.HydraMaxPerUpdate.Value) && (DupeQueue.Count != 0); i++)
            {
                var dupeInfo = DupeQueue.Dequeue();

                InstantiateDupe(dupeInfo);
            }

            InstantiatedThisTick = 0;
            NumFixedUpdates += 1;
        }

        public static void EnqueueDupe(QueuedDupeInfo dupeInfo)
        {
            if (InstantiatedThisTick >= Options.HydraMaxPerUpdate.Value)
            {
                DupeQueue.Enqueue(dupeInfo);
            }
            else
            {
                ImmediatelyDupeStack.Push(dupeInfo);
            }
        }

        private static void PreEnemyDeath(EnemyComponents enemy, bool instakill)
        {
            var go = enemy.gameObject;
            var ehm = enemy.GetHydraComp();

            ehm.NotifyOfDeath(instakill);
        }

        private static void PostEnemyDeath(EnemyComponents enemy, bool instakill)
        {
        }

        private static void DuringEnemyDeath(EnemyComponents enemy)
        {
            var go = enemy.gameObject;
            var ehm = enemy.GetHydraComp();
            ehm.NotifyOfDeath(false);
            ehm.DuringDeath();
        }

        private static Queue<QueuedDupeInfo> DupeQueue = new Queue<QueuedDupeInfo>(256);
        private static Stack<QueuedDupeInfo> ImmediatelyDupeStack = new Stack<QueuedDupeInfo>(256);

        private static void LateUpdate()
        {
            if (NyxLib.Cheats.IsCheatDisabled(Cheats.HydraMode))
            {
                return;
            }

            while (ImmediatelyDupeStack.Count > 0)
            {
                InstantiateDupe(ImmediatelyDupeStack.Pop());
            }
        }

        public static void InstantiateDupe(QueuedDupeInfo dupeInfo)
        {
            InstantiatedThisTick += 1;
            var dupeGo = dupeInfo.CloneStore.GetNewInstance(dupeInfo.CloneParent);
            GameObject malFaceDupeGo = null;
            EnemyComponents enemy;

            if (dupeInfo.EnemyType == EnemyType.MaliciousFace)
            {
                malFaceDupeGo = dupeGo;
                enemy = dupeGo.GetComponentInChildren<EnemyComponents>();
                Assert.IsNotNull(enemy);
                dupeGo = enemy.gameObject;
            }
            else
            {
                Assert.IsNotNull(dupeGo);
                enemy = dupeGo.GetComponent<EnemyComponents>();
            }

            dupeGo.transform.position = dupeInfo.Position;
            dupeGo.transform.rotation = dupeInfo.Rotation;

            dupeGo.SetActive(true);
            malFaceDupeGo?.SetActive(true);
            var eid = dupeGo.GetComponent<EnemyIdentifier>();

            eid.spawnIn = false;
            eid.timeSinceSpawned = 0.0f;

            Assert.IsNotNull(enemy);
            Assert.IsNotNull(enemy.GetHydraComp());
            Assert.IsNotNull(enemy.GetHydraComp().Shared);

            enemy.GetHydraComp().Depth = dupeInfo.Depth;

            if (dupeInfo.BossBar)
            {
                eid.BossBar(true);
            }

            dupeInfo.CloneStore.RemoveReservation();
        }
    }
}