#define UNITY_ANALYTICS2

using System;
using Unity.Services.Analytics.Internal;
using Unity.Services.Analytics.Platform;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Device.Internal;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.Analytics
{
    public static partial class Events
    {
        const string s_CollectUrlPattern = "https://collect.analytics.unity3d.com/collect/api/project/{0}/{1}";

        internal static IPlayerId PlayerId { get; set; }
        internal static IInstallationId InstallId { get; set; }

        internal static Analytics.Internal.Buffer dataBuffer = new Analytics.Internal.Buffer();
        internal static Dispatcher dataDispatcher = new Dispatcher(dataBuffer);
        
        public static Analytics.Internal.Buffer Buffer
        {
            get { return dataBuffer; }
        }
        
        static UnityWebRequestAsyncOperation s_Request;
        static string s_CollectURL;
        static string s_SessionID;
        static Data.StdCommonParams s_CommonParams = new Data.StdCommonParams();
        static Ua2CoreInitializeCallback s_coreGlue = new Ua2CoreInitializeCallback();
        static string s_StartUpCallingId = "com.unity.services.analytics.Events.Startup";

        static Events()
        {
            // The docs say nothing about Application.cloudProjectId being guaranteed or not,
            // we add a check just to be sure.
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Debug.LogError("No Cloud ProjectID Found for Analytics");
                return;
            }

            s_SessionID = Guid.NewGuid().ToString();
            
            s_CommonParams.ClientVersion = Application.version;
            s_CommonParams.GameBundleID = Application.identifier;
            s_CommonParams.Platform = Platform.Runtime.Name();
            s_CommonParams.BuildGuuid = Application.buildGUID;
            s_CommonParams.Idfv = DeviceIdentifiers.Idfv;
            
            SetVariableCommonParams();
            
            DeviceIdentifiers.SetupIdentifiers();
            
            #if UNITY_ANALYTICS_DEVELOPMENT
            Debug.LogFormat("UA2 Setup\nSessionID:{0}", s_SessionID);
            #endif
        }

        /// <summary>
        /// Sets up the internals of the Analytics SDK, including the regular sending of events and assigning
        /// the userID to be used in further event recording.
        /// </summary>
        internal static void Startup()
        {
            // Startup Events.
            Data.Generator.SdkStartup(ref dataBuffer, DateTime.UtcNow, s_CommonParams, s_StartUpCallingId);
            Data.Generator.ClientDevice(ref dataBuffer, DateTime.UtcNow, s_CommonParams, s_StartUpCallingId, SystemInfo.processorType, SystemInfo.graphicsDeviceName, SystemInfo.processorCount, SystemInfo.systemMemorySize, Screen.width, Screen.height, (int)Screen.dpi);

            #if UNITY_DOTSRUNTIME
            bool isTiny = true;
            #else
            bool isTiny = false;
            #endif

            Data.Generator.GameStarted(ref dataBuffer, DateTime.UtcNow, s_CommonParams, s_StartUpCallingId, "PlayerSettings.productGUID.ToString()", SystemInfo.operatingSystem, isTiny, Platform.DebugDevice.IsDebugDevice(), Locale.CurrentCulture().Name);
        }

        internal static void SetEnvironment(string environment)
        {
            s_CollectURL = String.Format(s_CollectUrlPattern, Application.cloudProjectId, environment.ToLowerInvariant());
                
            #if UNITY_ANALYTICS_DEVELOPMENT
            Debug.LogFormat("UA2 Setup\nCollectURL:{0}", s_CollectURL);
            #endif
        }

        internal static void NewPlayerEvent()
        {
            if (InstallId != null && new NewPlayerHelper(InstallId).IsNewPlayer())
            {
                Data.Generator.NewPlayer(ref dataBuffer, DateTime.UtcNow, s_CommonParams, s_StartUpCallingId, SystemInfo.deviceModel);
            }
        }

        /// <summary>
        /// Shuts down the Analytics SDK, including preventing the further upload of events.
        /// </summary>
        internal static void Shutdown()
        {
            Data.Generator.GameEnded(ref dataBuffer, DateTime.UtcNow, s_CommonParams, "com.unity.services.analytics.Events.Shutdown", Data.Generator.SessionEndState.QUIT);
            Flush();
        }

        // <summary>
        // Internal tick is called by the Heartbeat at set intervals.
        // </summary>
        internal static void InternalTick()
        {
            SetVariableCommonParams();
            Data.Generator.GameRunning(ref dataBuffer, DateTime.UtcNow, s_CommonParams, "com.unity.services.analytics.Events.InternalTick");
            Flush();
        }
        
        // <summary>
        // Forces an internal tick to flush out data.
        // </summary>
        public static void Flush()
        {
            if (InstallId == null)
            {
                #if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("The Core callback hasn't yet triggered.");
                #endif
                
                return;
            }
            dataBuffer.UserID = InstallId.GetOrCreateIdentifier();
            dataBuffer.SessionID = s_SessionID;
            dataDispatcher.CollectUrl = s_CollectURL;
            dataDispatcher.Flush();
        }

        static void SetVariableCommonParams()
        {
            s_CommonParams.UserCountry = Analytics.Internal.Platform.UserCountry.Name();
            s_CommonParams.Idfa = DeviceIdentifiers.Idfa;
            s_CommonParams.DeviceVolume = DeviceVolumeProvider.GetDeviceVolume();
            s_CommonParams.BatteryLoad = SystemInfo.batteryLevel;
            s_CommonParams.UasUserID = PlayerId != null ? PlayerId.PlayerId : null;
        }

        static void GameEnded(Data.Generator.SessionEndState quitState)
        {
            Data.Generator.GameEnded(ref dataBuffer, DateTime.UtcNow, s_CommonParams,"com.unity.services.analytics.Events.GameEnded", quitState);
	      }

    }
}
