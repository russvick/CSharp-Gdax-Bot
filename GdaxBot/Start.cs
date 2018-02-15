using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using GdaxBot.Models;
namespace GdaxBot
{
    public static class Start
    {
        static List<EmaSmaCrossoverModel> tradingList = new List<EmaSmaCrossoverModel>();
        static List<Trading> tradingBlock = new List<Trading>();
        static Start()
        {
            EmaSmaCrossoverModel t1 = new EmaSmaCrossoverModel(15, 60, 10, 60);
            EmaSmaCrossoverModel t2 = new EmaSmaCrossoverModel(10, 60, 30, 60);
            tradingBlock.Add(new Trading());
            tradingBlock.Add(new Trading());
            tradingList.Add(t1);
            tradingList.Add(t2);
        }
        

            
        [FunctionName("Start")]
        public static void Run([TimerTrigger("2 */0 * * * *")]TimerInfo myTimer, TraceWriter log)
        //public static void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {          
            for(int i = 0; i < tradingBlock.Count; i++)
            {
                int smaT = tradingList[i].SmaTimePeriod;
                int smaG = tradingList[i].SmaGranularity;
                int emaT = tradingList[i].EmaTimePeriod;
                int emaG = tradingList[i].EmaGranularity;

                var tradingTup = tradingBlock[i].EmaSmaCrossover(smaT, smaG, emaT, smaG);

                if(tradingTup.Item3 > 0)
                {
                    log.Info($"\nActivity on: trading block: {i+1} [{smaT/emaT}]\nLast Ema:{tradingTup.Item1}\nLast Sma: {tradingTup.Item2}\nCurrentFunds: {tradingTup.Item3}\n----------------------");
                }
                else
                {
                    log.Info($"\nRunning trading block: {i+1}\n----------------------");
                }
            }
        }
    }
}
