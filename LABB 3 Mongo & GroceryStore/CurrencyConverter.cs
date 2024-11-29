using System;
using System.Collections.Generic;
using MongoDB.Driver;


namespace Simple.Store
{
    public static class CurrencyConverter
    {
        // Dictionary of exchange rates, with SEK as the base currency
        private static readonly Dictionary<string, decimal> exchangeRates = new Dictionary<string, decimal>
        {
            { "SEK", 1.0m },   // Base currency (SEK)
            { "EUR", 0.093m },  // 1 SEK = 0.093 EUR
            { "CHF", 0.10m }    // 1 SEK = 0.10 CHF
        };

        // Convert a given amount from one currency to another
        public static decimal ConvertAmount(decimal amount, string fromCurrency, string toCurrency)
        {
            // Validate the currencies
            if (!exchangeRates.ContainsKey(fromCurrency))
            {
                Console.WriteLine($"Invalid 'from' currency: {fromCurrency}");
                return -1;  // Return -1 to indicate error
            }

            if (!exchangeRates.ContainsKey(toCurrency))
            {
                Console.WriteLine($"Invalid 'to' currency: {toCurrency}");
                return -1;  // Return -1 to indicate error
            }

            // Convert the amount to SEK if not already in SEK
            decimal amountInSEK = fromCurrency == "SEK" ? amount : amount / exchangeRates[fromCurrency];

            // Convert from SEK to the desired currency
            decimal convertedAmount = amountInSEK * exchangeRates[toCurrency];

            return convertedAmount;
        }

        // Convert a given amount to EUR (from any supported currency)
        public static decimal ConvertToEUR(decimal amount, string fromCurrency)
        {
            return ConvertAmount(amount, fromCurrency, "EUR");
        }

        // Convert a given amount to CHF (from any supported currency)
        public static decimal ConvertToCHF(decimal amount, string fromCurrency)
        {
            return ConvertAmount(amount, fromCurrency, "CHF");
        }
    }
}