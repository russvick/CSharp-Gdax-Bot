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

        static SMA sma = new SMA()
        {
            Granularity = 60,
            TimePeriod = 5
        };

        static Start()
        {
            tradingBlock.Add(new Trading(3, 15, 60, 10, 60, ProductId.BTCUSD));
            tradingBlock.Add(new Trading(5, 15, 60, 10, 60, ProductId.LTCUSD));
            tradingBlock.Add(new Trading(5, 10, 60, 30, 60, ProductId.LTCUSD));
            tradingBlock.Add(new Trading(5, 10, 60, 30, 60, ProductId.BTCUSD));
        }

        [FunctionName("Start")]
        public static void Run([TimerTrigger("2 */0 * * * *")]TimerInfo myTimer, TraceWriter log)
        //public static void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {                  
            var currBlock = tradingBlock;
            foreach(var item in tradingBlock)
            {
                var tradingTup = item.EmaSmaCrossover().Result;
                var lst = new List<SMA>();
                lst.Add(sma); 

                var tradingSlope = item.SMATripleCrossover(sma);
                if (tradingTup.Item3 > 0)
                {
                    log.Info($"\nActivity on: [{item.SmaTimePeriod / item.EmaTimePeriod}]" + "" +
                        $"\nLast Ema:{tradingTup.Item1}\nLast Sma: {tradingTup.Item2}\nCurrentFunds: {tradingTup.Item3}\n----------------------");
                }
            }                   
        }
    }
}
