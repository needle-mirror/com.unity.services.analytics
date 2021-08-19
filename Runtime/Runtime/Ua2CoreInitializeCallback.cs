using System;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Device.Internal;
using System.Threading.Tasks;
using Unity.Services.Core.Environments.Internal;

class Ua2CoreInitializeCallback : IInitializablePackage
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        CoreRegistry.Instance.RegisterPackage(new Ua2CoreInitializeCallback())
            .DependsOn<IInstallationId>()
            .OptionallyDependsOn<IPlayerId>();
    }

    public Task Initialize(CoreRegistry registry)
    {
        IInstallationId installationId = registry.GetServiceComponent<IInstallationId>();
        Events.InstallId = installationId;
        
        IPlayerId playerId = CoreRegistry.Instance.GetServiceComponent<IPlayerId>();
        Events.PlayerId = playerId;

        IEnvironments environments = CoreRegistry.Instance.GetServiceComponent<IEnvironments>();
        Events.SetEnvironment(environments.Current);
        
        #if UNITY_ANALYTICS_DEVELOPMENT
        Debug.LogFormat("Core Initialize Callback\nInstall ID: {0}\nPlayer ID: {1}",
            installationId.GetOrCreateIdentifier(),
            playerId.PlayerId);
        #endif
        
        Events.NewPlayerEvent();

        return Task.CompletedTask;
    }
}
