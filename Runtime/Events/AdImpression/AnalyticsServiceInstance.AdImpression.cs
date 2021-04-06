using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        /// <summary>
        /// Record an Ad Impression event.
        /// </summary>
        /// <param name="adImpressionParameters">(Required) Helper object to handle arguments.</param>
        public void AdImpression(AdImpressionParameters adImpressionParameters)
        {
            if (!ServiceEnabled)
            {
                return;
            }

            Debug.Assert(!string.IsNullOrEmpty(adImpressionParameters.PlacementID), "Required to have a value for placement ID.");
            Debug.Assert(!string.IsNullOrEmpty(adImpressionParameters.PlacementName), "Required to have a value for placement name.");
            _ = adImpressionParameters.AdCompletionStatus.ToString().ToUpperInvariant();
            _ = adImpressionParameters.AdProvider.ToString().ToUpperInvariant();

            dataGenerator.AdImpression(ref dataBuffer, DateTime.UtcNow, m_CommonParams, "com.unity.services.analytics.events.adimpression", adImpressionParameters);
        }
    }
}
