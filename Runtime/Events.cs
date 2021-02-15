#define UNITY_ANALYTICS2

using System;
using System.Threading.Tasks;
using Unity.Services.Analytics.Internal;
using Unity.Services.Analytics.Platform;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Device.Internal;
using UnityEngine;

namespace Unity.Services.Analytics
{
    public static partial class Events
    {
        const string k_CollectUrlPattern = "https://collect.analytics.unity3d.com/collect/api/project/{0}/{1}";
        const string k_ForgetCallingId = "com.unity.services.analytics.Events." + nameof(OptOut);

        internal static IPlayerId PlayerId { get; private set; }
        internal static IInstallationId InstallId { get; private set; }

        internal static string CustomAnalyticsId { get; private set; }

        internal static IBuffer dataBuffer = new Internal.Buffer();
        internal static IDispatcher dataDispatcher { get; set; }

        [Obsolete("Use the provided event methods to send events - the buffer will be removed in a future release.")]
        public static IBuffer Buffer => dataBuffer;

        /// <summary>
        /// This is the URL for the Unity Analytics privacy policy. This policy page should
        /// be presented to the user in a platform-appropriate way along with the ability to
        /// opt out of data collection.
        /// </summary>
        public static readonly string PrivacyUrl = "https://unity3d.com/legal/privacy-policy";

        static string s_CollectURL;
        static readonly string s_SessionID;
        static readonly Data.StdCommonParams s_CommonParams = new Data.StdCommonParams();
        static readonly string s_StartUpCallingId = "com.unity.services.analytics.Events.Startup";

        internal static IAnalyticsForgetter s_AnalyticsForgetter;

        static Events()
        {
            // The docs say nothing about Application.cloudProjectId being guaranteed or not,
            // we add a check just to be sure.
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Debug.LogError("No Cloud ProjectID Found for Analytics");
                return;
            }

            dataDispatcher = new Dispatcher(dataBuffer, ConsentTracker);

            s_SessionID = Guid.NewGuid().ToString();

            s_CommonParams.ClientVersion = Application.version;
            s_CommonParams.ProjectID = Application.cloudProjectId;
            s_CommonParams.GameBundleID = Application.identifier;
            s_CommonParams.Platform = Runtime.Name();
            s_CommonParams.BuildGuuid = Application.buildGUID;
            s_CommonParams.Idfv = DeviceIdentifiersInternal.Idfv;

            SetVariableCommonParams();

            DeviceIdentifiersInternal.SetupIdentifiers();
        }

        internal static void SetDependencies(IInstallationId installId, IPlayerId playerId, string environment, string customAnalyticsId)
        {
            InstallId = installId;
            PlayerId = playerId;
            CustomAnalyticsId = customAnalyticsId;

            s_CollectURL = String.Format(k_CollectUrlPattern, Application.cloudProjectId, environment.ToLowerInvariant());
        }

        internal static async Task Initialize(IInstallationId installId, IPlayerId playerId, string environment, string customAnalyticsId)
        {
            SetDependencies(installId, playerId, environment, customAnalyticsId);

#if UNITY_ANALYTICS_DEVELOPMENT
            Debug.LogFormat("UA2 Setup\nCollectURL:{0}\nSessionID:{1}", s_CollectURL, s_SessionID);
#endif

            try
            {
                await ConsentTracker.CheckGeoIP();

                if (ConsentTracker.IsGeoIpChecked() && (ConsentTracker.IsConsentDenied() || ConsentTracker.IsOptingOutInProgress()))
                {
                    OptOut();
                }
            }
            catch (ConsentCheckException e)
            {
#if UNITY_ANALYTICS_EVENT_LOGS
                Debug.Log("Initial GeoIP lookup fail: " + e.Message);
#endif
            }
        }

        /// <summary>
        /// Opts the user out of sending analytics from all legislations.
        /// To deny consent for a specific opt-in legislation, like PIPL, use `ProvideConsent(string key, bool consent)` method)
        /// All existing cached events and any subsequent events will be discarded immediately.
        /// A final 'forget me' signal will be uploaded which will trigger purge of analytics data for this user from the back-end.
        /// If this 'forget me' event cannot be uploaded immediately (e.g. due to network outage), it will be reattempted regularly
        /// until successful upload is confirmed.
        /// Consent status is stored in PlayerPrefs so that the opted-out status is maintained over app restart.
        /// This action cannot be undone.
        /// </summary>
        /// <exception cref="ConsentCheckException">Thrown if the required consent flow cannot be determined..</exception>
        public static void OptOut()
        {
            Debug.Log(ConsentTracker.IsConsentDenied()
                ? "This user has opted out. Any cached events have been discarded and no more will be collected."
                : "This user has opted out and is in the process of being forgotten...");

            if (ConsentTracker.IsConsentGiven())
            {
                // We have revoked consent but have not yet sent the ForgetMe signal
                // Thus we need to keep some of the dispatcher alive until that is done
                ConsentTracker.BeginOptOutProcess();
                RevokeWithForgetEvent();

                return;
            }

            if (ConsentTracker.IsOptingOutInProgress())
            {
                RevokeWithForgetEvent();
                return;
            }

            Revoke();
            ConsentTracker.SetDenyConsentToAll();
        }

        static void Revoke()
        {
            // We have already been forgotten and so do not need to send the ForgetMe signal
            dataBuffer.ClearDiskCache();
            dataBuffer = new BufferRevoked();
            dataDispatcher = new Dispatcher(dataBuffer);
            ContainerObject.DestroyContainer();
        }

        internal static void RevokeWithForgetEvent()
        {
            // Clear everything out of the real buffer and replace it with a dummy
            // that will swallow all events and do nothing
            dataBuffer.ClearBuffer();
            dataBuffer = new BufferRevoked();
            dataDispatcher = new Dispatcher(dataBuffer);

            s_AnalyticsForgetter = new AnalyticsForgetter(s_CollectURL,
                InstallId.GetOrCreateIdentifier(),
                Internal.Buffer.SaveDateTime(DateTime.UtcNow),
                k_ForgetCallingId,
                ForgetMeEventUploaded, ConsentTracker);
            s_AnalyticsForgetter.AttemptToForget();
        }

        internal static void ForgetMeEventUploaded()
        {
            ContainerObject.DestroyContainer();
            ConsentTracker.FinishOptOutProcess();

#if UNITY_ANALYTICS_EVENT_LOGS
            Debug.Log("User opted out successfully and has been forgotten!");
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
            var isTiny = false;
#endif

            Data.Generator.GameStarted(ref dataBuffer, DateTime.UtcNow, s_CommonParams, s_StartUpCallingId, Application.buildGUID, SystemInfo.operatingSystem, isTiny, DebugDevice.IsDebugDevice(), Locale.AnalyticsRegionLanguageCode());
        }

        internal static void NewPlayerEvent()
        {
            if (InstallId != null && new InternalNewPlayerHelper(InstallId).IsNewPlayer())
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
            if (ConsentTracker.IsGeoIpChecked())
            {
                Flush();
            }
        }

        // <summary>
        // Internal tick is called by the Heartbeat at set intervals.
        // </summary>
        internal static void InternalTick()
        {
            SetVariableCommonParams();
            Data.Generator.GameRunning(ref dataBuffer, DateTime.UtcNow, s_CommonParams, "com.unity.services.analytics.Events.InternalTick");
            if (ConsentTracker.IsGeoIpChecked())
            {
                Flush();
            }
        }

        /// <summary>
        /// Forces an immediately upload of all recorded events to the server, if there is an internet connection.
        /// </summary>
        /// <exception cref="ConsentCheckException">Thrown if the required consent flow cannot be determined..</exception>
        public static void Flush()
        {
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                return;
            }

            if (InstallId == null)
            {
#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("The Core callback hasn't yet triggered.");
#endif

                return;
            }

            if (ConsentTracker.IsGeoIpChecked() && ConsentTracker.IsConsentGiven())
            {
                dataBuffer.InstallID = InstallId.GetOrCreateIdentifier();
                dataBuffer.PlayerID = PlayerId?.PlayerId;

                dataBuffer.UserID = !string.IsNullOrEmpty(CustomAnalyticsId) ? CustomAnalyticsId : dataBuffer.InstallID;

                dataBuffer.SessionID = s_SessionID;
                dataDispatcher.CollectUrl = s_CollectURL;
                dataDispatcher.Flush();
            }

            if (ConsentTracker.IsOptingOutInProgress())
            {
                s_AnalyticsForgetter.AttemptToForget();
            }
        }

        static void SetVariableCommonParams()
        {
            s_CommonParams.Idfa = DeviceIdentifiersInternal.Idfa;
            s_CommonParams.DeviceVolume = DeviceVolumeProvider.GetDeviceVolume();
            s_CommonParams.BatteryLoad = SystemInfo.batteryLevel;
            s_CommonParams.UasUserID = PlayerId != null ? PlayerId.PlayerId : null;
        }

        static void GameEnded(Data.Generator.SessionEndState quitState)
        {
            Data.Generator.GameEnded(ref dataBuffer, DateTime.UtcNow, s_CommonParams, "com.unity.services.analytics.Events.GameEnded", quitState);
        }
    }
}
