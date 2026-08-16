using System;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.Hydra
{
    [Serializable]
    public class FleshPanopticonHydra : MonoBehaviour
    {
        public class SharedData : ScriptableObject
        {
            public uint NumLights = 0;
        }

        EnemyHydra Hydra = null;
        SharedData Shared = null;
        bool ContributingLights = false;

        protected void Start()
        {
            Hydra = GetComponent<EnemyHydra>();
            Shared = (SharedData)(Hydra.Shared.EnemySpecificShared);

            if (Hydra.Depth > 0 && Shared.NumLights > 2)
            {
                var lights = transform.GetComponentsInChildren<Light>();

                foreach (var light in lights)
                {
                    if (light.gameObject.name.Contains("Spot"))
                    {
                        Destroy(light);
                    }
                }
            }
            else
            {
                Shared.NumLights += 1;
                ContributingLights = true;
            }

            Hydra.PreDeath += PreDeath;
        }

        private void PreDeath()
        {
            if (ContributingLights)
            {
                ContributingLights = false;
                Shared.NumLights -= 1;
            }

            FleshPrison fleshPrison = GetComponent<FleshPrison>();

            foreach (var drone in fleshPrison.currentDrones)
            {
                if (drone == null)
                {
                    continue;
                }

                if (drone.GetComponent<FleshDroneHydra>() == null)
                {
                    drone.GetComponent<EnemyIdentifier>()?.InstaKill();
                    continue;
                }

                drone.GetComponent<FleshDroneHydra>().NotifyPrisonDied();
            }
        }

        private void FixedUpdate()
        {
            FleshPrison fleshPrison = GetComponent<FleshPrison>();

            if (fleshPrison.currentDrones.Count > 0)
            {
                foreach (var drone in fleshPrison.currentDrones)
                {
                    if (drone == null)
                    {
                        continue;
                    }

                    var fdHydra = drone.GetComponent<FleshDroneHydra>();

                    if (fdHydra != null)
                    {
                        if (fdHydra.Shared == null)
                        {
                            continue;
                        }

                        fdHydra.Shared.Prison = fleshPrison;
                    }

                    if (!ContributingLights)
                    {
                        fdHydra.QueueDeath();
                    }
                }
            }
        }
    }
}