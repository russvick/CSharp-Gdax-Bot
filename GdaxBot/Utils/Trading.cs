using GdaxBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdaxBot
{
    class Trading
    {
        private static double _lastSma = 0;
        private static double _lastEma = 0;
        private static int boughtOrSold = 0;
        private static APIConnect _api;
        private static double funds = 10000;
        private static double btcHolding = 0.0;
        private static int counter = 0;
        private static string mydocpath = @"C:\Users\Russ\Desktop\TransactionLog";

        public double LastSma { get { return _lastSma; } }
        public double LastEma { get { return _lastEma; } }
        public double Funds { get { return funds; } }
        public int EmaTimePeriod { get; set; }
        public int EmaGranularity { get; set; }

        public int SmaTimePeriod { get; set; }
        public int SmaGranularity { get; set; }

        public Trading()
        {
            _api = new APIConnect();
        }

        public void Process()
        {
            var movingAvg = _api.GetMovingAverages(15, 60, 10, 60).Result;
            double sma = movingAvg.Item1;
            double ema = movingAvg.Item2;

            //If this is our first run then we will send a buy order
            if (_lastEma == 0 && _lastSma == 0)
            {
                _lastSma = sma;
                _lastEma = ema;
                boughtOrSold = 1;
                BuyCoin(100, ema, sma);
            }
            else if ((_lastSma < _lastEma && sma > ema) || (_lastSma > _lastEma && sma < ema))
            {
                //if out last order was buy (1) then we will sell
                if (boughtOrSold == 1)
                {
                    boughtOrSold = 0;
                    SellCoin(100, ema, sma);
                }
                else
                {
                    boughtOrSold = 1;
                    BuyCoin(100, ema, sma);
                }

                _lastSma = sma;
                _lastEma = ema;
            }
            counter++;
        }

        //Buy coin at current price
        public void BuyCoin(double buyAmount, double ema, double sma)
        {
            var btcPrice = _api.CoinPrice;

            funds -= buyAmount;

            btcHolding += buyAmount / btcPrice;

            using (StreamWriter w = File.AppendText(mydocpath))
            {
                Log($"Bought BTC at: ${btcPrice}\r\nEma Price: {ema}\r\nSma Price: {sma}", w);
            }
        }

        //Sell coin at current price
        public void SellCoin(double sellAmount, double ema, double sma)
        {
            string transactionDescription = string.Empty;

            var btcPrice = _api.CoinPrice;

            btcHolding -= sellAmount / btcPrice;

            funds += btcPrice / sellAmount;

            using (StreamWriter w = File.AppendText(mydocpath))
            {
                Log($"Sold BTC at: ${btcPrice}\r\nEma Price: {ema}\r\nSma Price: {sma}", w);
            }
        }

        public static void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }
    }
}
