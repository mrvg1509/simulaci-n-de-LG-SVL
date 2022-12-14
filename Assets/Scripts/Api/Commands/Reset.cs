/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Api.Commands
{
    class Reset : ICommand
    {
        public string Name { get { return "simulator/reset"; } }

        public static void Run()
        {
            foreach (var kv in ApiManager.Instance.Agents)
            {
                var obj = kv.Value;
                var setup = obj.GetComponent<AgentSetup>();
                if (setup != null)
                {
                    var sensors = setup.GetSensors();
                    foreach (var sensor in sensors)
                    {
                        var suid = ApiManager.Instance.SensorUID[sensor];
                        ApiManager.Instance.Sensors.Remove(suid);
                        ApiManager.Instance.SensorUID.Remove(sensor);
                    }

                    SimulatorManager.Instance.DespawnVehicle(setup.Connector);
                    ROSAgentManager.Instance.RemoveVehicleObject(obj);
                    Object.Destroy(obj);
                }

                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc != null)
                {
                    NPCManager.Instance.DespawnVehicle(npc);
                }

                var ped = obj.GetComponent<PedestrianComponent>();
                if (ped != null)
                {
                    PedestrianManager.Instance.DespawnPedestrianApi(ped);
                }
            }

            ApiManager.Instance.Reset();
        }

        public void Execute(JSONNode args)
        {
            Run();
            ApiManager.Instance.SendResult();
        }
    }
}
