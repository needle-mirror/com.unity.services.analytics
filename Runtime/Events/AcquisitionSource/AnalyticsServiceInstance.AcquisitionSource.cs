using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        /// <summary>
        /// Record an acquisitionSource event.
        /// </summary>
        /// <param name="acquisitionSourceParameters">(Required) Helper object to handle parameters.</param>
        public void AcquisitionSource(AcquisitionSourceParameters acquisitionSourceParameters)
        {
            if (!ServiceEnabled)
            {
                return;
            }

            Debug.Assert(!string.IsNullOrEmpty(acquisitionSourceParameters.Channel),
                "Required to have a value for channel");
            Debug.Assert(!string.IsNullOrEmpty(acquisitionSourceParameters.CampaignId),
                "Required to have a value for campaignId");
            Debug.Assert(!string.IsNullOrEmpty(acquisitionSourceParameters.CreativeId),
                "Required to have a value for creativeId");
            Debug.Assert(!string.IsNullOrEmpty(acquisitionSourceParameters.CampaignName),
                "Required to have a value for campaignName");
            Debug.Assert(!string.IsNullOrEmpty(acquisitionSourceParameters.Provider),
                "Required to have a value for provider");

            dataGenerator.AcquisitionSource(ref dataBuffer, DateTime.Now, m_CommonParams,
                "com.unity.services.analytics.events.acquisitionSource", acquisitionSourceParameters);
        }
    }
}
