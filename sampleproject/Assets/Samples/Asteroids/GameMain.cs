using Unity.Entities;
using Unity.Networking.Transport;

using UnityEngine;

public class GameMain : UnityEngine.MonoBehaviour
{
    private bool isPlaying = false;
    private const ushort networkPort = 50001;

    public void Start()
    {
        ClientServerSystemManager.CollectAllSystems();
    }

    public void OnGUI()
    {
        if (!isPlaying)
        {
            if (GUI.Button(new Rect(100, 100, 200, 100), "Server"))
            {
                ClientServerSystemManager.InitServerSystems();

                World.Active.GetExistingSystem<TickServerSimulationSystem>().Enabled = true;
                var serverWorld = ClientServerSystemManager.serverWorld;
                var entityManager = serverWorld.EntityManager;
                var settings = entityManager.CreateEntity();
                var settingsData = GameSettings.settings;
                settingsData.InitArchetypes(entityManager);
                entityManager.AddComponentData(settings, settingsData);

                NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
                ep.Port = networkPort;
                serverWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);

                isPlaying = true;
            }

            if (GUI.Button(new Rect(100, 200, 200, 100), "Client"))
            {
                ClientServerSystemManager.InitClientSystems();

                World.Active.GetExistingSystem<TickClientSimulationSystem>().Enabled = true;
                World.Active.GetExistingSystem<TickClientPresentationSystem>().Enabled = true;
                var clientWorld = ClientServerSystemManager.clientWorld;
                var entityManager = clientWorld.EntityManager;
                var settingsData = new ClientSettings(entityManager);
                var settingsEnt = entityManager.CreateEntity();
                entityManager.AddComponentData(settingsEnt, settingsData);

                NetworkEndPoint ep = NetworkEndPoint.LoopbackIpv4;
                ep.Port = networkPort;
                clientWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);

                isPlaying = true;
            }

            if (GUI.Button(new Rect(100, 300, 200, 100), "ClientListenServer"))
            {
                ClientServerSystemManager.InitServerSystems();
                ClientServerSystemManager.InitClientSystems();

                {
                    World.Active.GetExistingSystem<TickServerSimulationSystem>().Enabled = true;
                    var serverWorld = ClientServerSystemManager.serverWorld;
                    var entityManager = serverWorld.EntityManager;
                    var settingsEnt = entityManager.CreateEntity();
                    var settingsCom = GameSettings.settings;
                    settingsCom.InitArchetypes(entityManager);
                    entityManager.AddComponentData(settingsEnt, settingsCom);
                    NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
                    ep.Port = networkPort;
                    serverWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
                }

                {
                    World.Active.GetExistingSystem<TickClientSimulationSystem>().Enabled = true;
                    World.Active.GetExistingSystem<TickClientPresentationSystem>().Enabled = true;
                    var clientWorld = ClientServerSystemManager.clientWorld;
                    var entityManager = clientWorld.EntityManager;
                    var settingsCom = new ClientSettings(entityManager);
                    var settingsEnt = entityManager.CreateEntity();
                    entityManager.AddComponentData(settingsEnt, settingsCom);

                    NetworkEndPoint ep = NetworkEndPoint.LoopbackIpv4;
                    ep.Port = networkPort;
                    clientWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
                }

                isPlaying = true;
            }
        }
    }
}
