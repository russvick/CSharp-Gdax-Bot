using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace GdaxBot
{
    public static class Start
    {
        [FunctionName("Start")]
        public static void Run([TimerTrigger("2 */0 * * * *")]TimerInfo myTimer, TraceWriter log)
        //public static void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            Trading t = new Trading();
            t.Process();
            log.Info($"\nLast Ema:{t.LastEma}\nLast Sma: {t.LastSma}\nCurrentFunds: {t.Funds}");
        }
    }
}
