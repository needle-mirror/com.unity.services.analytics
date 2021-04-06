using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        public void TransactionFailed(TransactionFailedParameters parameters)
        {
            if (!ServiceEnabled)
            {
                return;
            }

            Debug.Assert(!string.IsNullOrEmpty(parameters.TransactionName), "Required to have a value for transactionName");
            Debug.Assert(!parameters.TransactionType.Equals(TransactionType.INVALID), "Required to have a value for transactionType");
            Debug.Assert(!string.IsNullOrEmpty(parameters.FailureReason), "Required to have a failure reason in transactionFailed event");

            if (string.IsNullOrEmpty(parameters.PaymentCountry))
            {
                parameters.PaymentCountry = Internal.Platform.UserCountry.Name();
            }

            dataGenerator.TransactionFailed(ref dataBuffer, DateTime.Now, m_CommonParams, "com.unity.services.analytics.events.TransactionFailed", parameters);
        }
    }
}
