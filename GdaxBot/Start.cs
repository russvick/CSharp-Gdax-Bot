using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using GdaxBot.Models;
namespace GdaxBot
{
    public static class Start
    {
        static List<Trading> tradingBlock = new List<Trading>();

        static Start()
        {
            tradingBlock.Add(new Trading(3, 15, 60, 10, 60, ProductId.BTCUSD));
            //tradingBlock.Add(new Trading(5, 15, 60, 10, 60, ProductId.LTCUSD));
            //tradingBlock.Add(new Trading(5, 10, 60, 30, 60, ProductId.LTCUSD));
            //tradingBlock.Add(new Trading(5, 10, 60, 30, 60, ProductId.BTCUSD));
        }

        [FunctionName("Start")]
        public static void Run([TimerTrigger("2 */0 * * * *")]TimerInfo myTimer, TraceWriter log)
        //public static void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            var currBlock = tradingBlock;
            SMA sma = new SMA()
            {
                Granularity = 60,
                TimePeriod = 5
            };
            SMA sma1 = new SMA()
            {
                Granularity = 60,
                TimePeriod = 10
            };
            SMA sma2 = new SMA()
            {
                Granularity = 60,
                TimePeriod = 20
            };

            List<SMA> smas = new List<SMA>();
            smas.Add(sma);
            smas.Add(sma1);
            smas.Add(sma2);

            foreach (var item in tradingBlock)
            {
                //var tradingTup = item.EmaSmaCrossover().Result;               
                var tradingSlope = item.SMATripleCrossover(smas);
                if (!String.IsNullOrEmpty(tradingSlope))
                {
                    log.Info($"{tradingSlope}\n----------------------");
                }
                //if (tradingTup.Item3 > 0)
                //{
                //    log.Info($"\nActivity on: [{item.SmaTimePeriod / item.EmaTimePeriod}]" + "" +
                //        $"\nLast Ema:{tradingTup.Item1}\nLast Sma: {tradingTup.Item2}\nCurrentFunds: {tradingTup.Item3}\n----------------------");
                //}
            }                   
        }
    }
}
