using System;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Device.Internal;
using System.Threading.Tasks;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Environments.Internal;

class Ua2CoreInitializeCallback : IInitializablePackage
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        CoreRegistry.Instance.RegisterPackage(new Ua2CoreInitializeCallback())
            .DependsOn<IInstallationId>()
            .DependsOn<IEnvironments>()
            .DependsOn<IProjectConfiguration>()
            .OptionallyDependsOn<IPlayerId>();
    }

    public Task Initialize(CoreRegistry registry)
    {
        IInstallationId installationId = registry.GetServiceComponent<IInstallationId>();
        IPlayerId playerId = registry.GetServiceComponent<IPlayerId>();
        IEnvironments environments = registry.GetServiceComponent<IEnvironments>();
        IProjectConfiguration projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();

        string analyticsUserId = projectConfiguration.GetString("com.unity.services.core.analytics-user-id");

        Events.Initialize(installationId, playerId, environments.Current, analyticsUserId);

        #if UNITY_ANALYTICS_DEVELOPMENT
        Debug.LogFormat("Core Initialize Callback\nInstall ID: {0}\nPlayer ID: {1}\nCustom Analytics ID: {2}",
            installationId.GetOrCreateIdentifier(),
            playerId?.PlayerId,
            analyticsUserId
        );
        #endif

        Events.NewPlayerEvent();
        if (Events.ConsentTracker.IsGeoIpChecked())
        {
            Events.Flush();
        }

        return Task.CompletedTask;
    }
}
