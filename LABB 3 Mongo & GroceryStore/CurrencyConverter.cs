using System;
using System.Collections.Generic;
using MongoDB.Driver;


namespace Simple.Store
{
    public static class CurrencyConverter
    {
        //Convert a given amount from one currency to another
        public static decimal ConvertAmount(decimal amount, string fromCurrency, string toCurrency)
        {
            //Initialize exchange rates (example rates based on SEK)
            Dictionary<string, decimal> exchangeRates = new Dictionary<string, decimal>
            {
                { "SEK", 1.0m },      //Base currency
                { "EUR", 0.093m },    //1 SEK = 0.093 EUR
                { "CHF", 0.10m }      //1 SEK = 0.10 CHF
            };

            if (!exchangeRates.ContainsKey(fromCurrency) || !exchangeRates.ContainsKey(toCurrency))
            {
                Console.WriteLine("Invalid currency specified.");
                return 0;
            }

            //Convert the amount to SEK first if not already in SEK
            decimal amountInSEK = fromCurrency == "SEK" ? amount : amount / exchangeRates[fromCurrency];

            //Convert from SEK to the desired currency
            decimal convertedAmount = amountInSEK * exchangeRates[toCurrency];

            return convertedAmount;
        }

        //Convert a given amount to EUR
        public static decimal ConvertToEUR(decimal amount, string fromCurrency)
        {
            return ConvertAmount(amount, fromCurrency, "EUR");
        }

        //Convert a given amount to CHF
        public static decimal ConvertToCHF(decimal amount, string fromCurrency)
        {
            return ConvertAmount(amount, fromCurrency, "CHF");
        }
    }
}
