using GdaxBot.Models;
using GdaxBot.TradingData;
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
        private double _lastSma = 0;
        private double _lastEma = 0;
        private int boughtOrSold = 0;
        private CandleSticksStrategies _candlestickStragey;
        private double funds = 10000;
        private double btcHolding = 0.0;
        private int counter = 0;
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
            _candlestickStragey = new CandleSticksStrategies();
        }

        public Tuple<double,double,double> EmaSmaCrossover(int smaTimePeriod, int smaGranularity, int emaTimePeriod, int emaGranularity)
        {
            //for logging
            SmaTimePeriod = smaTimePeriod;
            SmaGranularity = smaGranularity;
            EmaTimePeriod = emaTimePeriod;
            EmaGranularity = emaGranularity;

            var movingAvg = _candlestickStragey.GetMovingAverages(smaTimePeriod, smaGranularity, emaTimePeriod, emaGranularity).Result;
            double sma = movingAvg.Item1;
            double ema = movingAvg.Item2;
            double buysellThreshold = Math.Abs(sma - ema);

            if (_lastEma == 0 && _lastSma == 0)
            {
                _lastSma = sma;
                _lastEma = ema;
            }

            //If this is our first run then set an sma/ema
            else if (buysellThreshold > 5)
            {
                if (_lastSma < _lastEma && sma > ema)
                {
                    boughtOrSold = 0;
                    SellCoin(100, ema, sma);
                    return new Tuple<double, double, double>(_lastSma, _lastEma, funds);
                }
                else if (_lastSma > _lastEma && sma < ema)
                {
                    boughtOrSold = 1;
                    BuyCoin(100, ema, sma);
                    return new Tuple<double, double, double>(_lastSma, _lastEma, funds);
                }

                _lastSma = sma;
                _lastEma = ema;
            }          
           
            counter++;
            return new Tuple<double, double, double>(_lastSma, _lastEma, 0);
        }
         
        //Buy coin at current price
        public void BuyCoin(double buyAmount, double ema, double sma)
        {
            var btcPrice = _candlestickStragey.CoinPrice;

            funds -= buyAmount;

            btcHolding += buyAmount / btcPrice;

            using (StreamWriter w = File.AppendText(mydocpath))
            {
                Log($"Ema/Sma: {SmaTimePeriod}" + "/" + 
                    $"{EmaTimePeriod}\r\nBought BTC at: ${btcPrice}\r\nEma Price: {ema}\r\nSma Price: {sma}\r\nCurrent Funds: {funds}", w);
            }
        }

        //Sell coin at current price
        public void SellCoin(double sellAmount, double ema, double sma)
        {
            string transactionDescription = string.Empty;

            var btcPrice = _candlestickStragey.CoinPrice;

            btcHolding -= sellAmount / btcPrice;

            funds += btcPrice / sellAmount;

            using (StreamWriter w = File.AppendText(mydocpath))
            {
                Log($"Ema/Sma: {SmaTimePeriod}" + "/"+ 
                    $"{EmaTimePeriod}\r\nSold BTC at: ${btcPrice}\r\nEma Price: {ema}\r\nSma Price: {sma}\r\nCurrent Funds: {funds}", w);
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
