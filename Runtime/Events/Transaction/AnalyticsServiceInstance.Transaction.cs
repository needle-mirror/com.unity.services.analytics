using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        readonly TransactionCurrencyConverter converter = new TransactionCurrencyConverter();

        public void Transaction(TransactionParameters transactionParameters)
        {
            if (!ServiceEnabled)
            {
                return;
            }

            Debug.Assert(!string.IsNullOrEmpty(transactionParameters.TransactionName), "Required to have a value for transactionName");
            Debug.Assert(!transactionParameters.TransactionType.Equals(TransactionType.INVALID), "Required to have a value for transactionType");

            // If The paymentCountry is not provided we will generate it.

            if (string.IsNullOrEmpty(transactionParameters.PaymentCountry))
            {
                transactionParameters.PaymentCountry = Internal.Platform.UserCountry.Name();
            }

            dataGenerator.Transaction(ref dataBuffer, DateTime.Now, m_CommonParams, "com.unity.services.analytics.events.transaction", transactionParameters);
        }

        public long ConvertCurrencyToMinorUnits(string currencyCode, double value)
        {
            return converter.Convert(currencyCode, value);
        }
    }
}
