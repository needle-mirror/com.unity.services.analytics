using UnityEngine;

namespace Unity.Services.Analytics.Platform
{
    public static class DeviceIdentifiers
    {
        public static string Idfv { get; private set; }
        public static string Idfa { get; private set; }

        public static void SetupIdentifiers()
        {
            Idfv = SystemInfo.deviceUniqueIdentifier;
            Application.RequestAdvertisingIdentifierAsync((id, enabled, error) =>
            {
                Idfa = id;
            });
        }
    }
}
