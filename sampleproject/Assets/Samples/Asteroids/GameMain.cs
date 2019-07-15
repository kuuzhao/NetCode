using Unity.Entities;
using Unity.Networking.Transport;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

#if !UNITY_CLIENT
[UpdateBefore(typeof(TickServerSimulationSystem))]
#endif
#if !UNITY_SERVER
[UpdateBefore(typeof(TickClientSimulationSystem))]
#endif
public class AsteroidsClientServerControlSystem : ComponentSystem
{
    private const ushort networkPort = 50001;
    private bool m_initializeClientServer;

    protected override void OnCreateManager()
    {
        var initEntity = EntityManager.CreateEntity();
        var group = GetEntityQuery(ComponentType.ReadWrite<GameSettingsComponent>());
        RequireForUpdate(group);
        m_initializeClientServer = true;

#if !UNITY_CLIENT
        if (ClientServerBootstrap.serverWorld != null)
        {
            World.GetOrCreateSystem<TickServerSimulationSystem>().Enabled = false;
        }
#endif
#if !UNITY_SERVER
        if (ClientServerBootstrap.clientWorld != null)
        {
            World.GetOrCreateSystem<TickClientSimulationSystem>().Enabled = false;
            World.GetOrCreateSystem<TickClientPresentationSystem>().Enabled = false;
        }
#endif
    }

    protected override void OnUpdate()
    {
        if (!m_initializeClientServer)
            return;
        m_initializeClientServer = false;
        // Bind the server and start listening for connections
#if !UNITY_CLIENT
        var serverWorld = ClientServerBootstrap.serverWorld;
        if (serverWorld != null)
        {
            World.GetExistingSystem<TickServerSimulationSystem>().Enabled = true;
            var entityManager = serverWorld.EntityManager;
            var settings = entityManager.CreateEntity();
            var settingsData = GetSingleton<ServerSettings>();
            settingsData.InitArchetypes(entityManager);
            entityManager.AddComponentData(settings, settingsData);
            NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
            ep.Port = networkPort;
            serverWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
        }
#endif
#if !UNITY_SERVER
        // Auto connect all clients to the server
        if (ClientServerBootstrap.clientWorld != null)
        {
            World.GetExistingSystem<TickClientSimulationSystem>().Enabled = true;
            World.GetExistingSystem<TickClientPresentationSystem>().Enabled = true;
            for (int i = 0; i < ClientServerBootstrap.clientWorld.Length; ++i)
            {
                var clientWorld = ClientServerBootstrap.clientWorld[i];
                var entityManager = clientWorld.EntityManager;
                var settings = new ClientSettings(entityManager);
                var settingsEnt = entityManager.CreateEntity();
                entityManager.AddComponentData(settingsEnt, settings);

                NetworkEndPoint ep = NetworkEndPoint.LoopbackIpv4;
                ep.Port = networkPort;
                clientWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
            }
        }
#endif
    }
}

public class GameMain : UnityEngine.MonoBehaviour
{
#if false
    private bool isPlaying = false;
    private List<System.Type> allSystems;
    public static World serverWorld;
    ServerSimulationSystemGroup serverSimulationSystemGroup;
    public static World clientWorld;
    ClientSimulationSystemGroup clientSimulationSystemGroup;
    ClientPresentationSystemGroup clientPresentationSystemGroup;

    public void Start()
    {
        // allSystems = GetAllSystems();
    }

    public void OnGUI()
    {
        if (!isPlaying)
        {
            if (GUI.Button(new Rect(100, 100, 200, 100), "Server"))
            {
                StartServer();
                isPlaying = true;
            }

            if (GUI.Button(new Rect(100, 200, 200, 100), "Client"))
            {
                StartClient();
                isPlaying = true;
            }

            if (GUI.Button(new Rect(100, 300, 200, 100), "ClientListenServer"))
            {
                StartServer();
                StartClient();
                isPlaying = true;
            }
        }
    }

    private List<System.Type> GetAllSystems()
    {
        var systemTypes = new List<System.Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!TypeManager.IsAssemblyReferencingEntities(assembly))
                continue;

            IReadOnlyList<Type> allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                allTypes = e.Types.Where(t => t != null).ToList();
                Debug.LogWarning(
                    $"GameMain failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : assembly.Location)}");
            }

            systemTypes.AddRange(allTypes.Where(FilterSystemType));
        }

        return systemTypes;
    }

    static bool FilterSystemType(Type type)
    {
        if (!type.IsSubclassOf(typeof(ComponentSystemBase)))
            return false;
        if (type.IsAbstract || type.ContainsGenericParameters)
            return false;
        if (type.GetConstructors().All(c => c.GetParameters().Length != 0))
            return false;

        return true;
    }

    private void StartServer()
    {
        serverWorld = new World("ServerWorld");
        serverSimulationSystemGroup = serverWorld.GetOrCreateSystem<ServerSimulationSystemGroup>();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(serverWorld);
    }

    private void StartClient()
    {
        clientWorld = new World("ClientWorld");
        clientSimulationSystemGroup = new ClientSimulationSystemGroup();
        clientPresentationSystemGroup = new ClientPresentationSystemGroup();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(clientWorld);
    }

    private void ProcessSystemInGroups()
    {
        foreach (var type in allSystems)
        {
            var groups = type.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);

            foreach (var grp in groups)
            {
                var group = grp as UpdateInGroupAttribute;
                if (group.GroupType == typeof(ClientAndServerSimulationSystemGroup) || group.GroupType == typeof(ServerSimulationSystemGroup))
                {
                    if (serverWorld != null)
                        serverSimulationSystemGroup.AddSystemToUpdateList(serverWorld.GetOrCreateSystem(type) as ComponentSystemBase);
                }

                if (group.GroupType == typeof(ClientAndServerSimulationSystemGroup) || group.GroupType == typeof(ClientSimulationSystemGroup))
                {
                    if (clientWorld != null)
                    {
                        clientSimulationSystemGroup
                            .AddSystemToUpdateList(clientWorld.GetOrCreateSystem(type) as ComponentSystemBase);
                    }
                }

                if (group.GroupType == typeof(ClientPresentationSystemGroup))
                {
                    if (clientWorld != null)
                    {
                        clientPresentationSystemGroup
                            .AddSystemToUpdateList(clientWorld.GetOrCreateSystem(type) as ComponentSystemBase);
                    }
                }

                if (group.GroupType != typeof(ClientAndServerSimulationSystemGroup) &&
                    group.GroupType != typeof(ServerSimulationSystemGroup) &&
                    group.GroupType != typeof(ClientSimulationSystemGroup) &&
                    group.GroupType != typeof(ClientPresentationSystemGroup))
                {

                }
            }
        }
    }
#endif
}
