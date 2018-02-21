using GdaxBot.Models;
using GdaxBot.TradingData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GdaxBot
{
    class Trading
    {
        private double _lastSma = 0;
        private double _lastEma = 0;
        private double funds = 10000;
        private double btcHolding = 0.0;
        private int counter = 0;
        private static string mydocpath = @"C:\Users\Russ\Desktop\TransactionLog";
        private SellAction _lastAction = SellAction.None;
        private List<SMA> _smas;
        private List<EMA> _emas;

        public double LastSma { get { return _lastSma; } }
        public double LastEma { get { return _lastEma; } }
        public double Funds { get { return funds; } }
        public int BuySellThreshold { get; set; }
        public int EmaTimePeriod { get; set; }
        public int EmaGranularity { get; set; }
        public string CoinType { get; set; }

        public int SmaTimePeriod { get; set; }
        public int SmaGranularity { get; set; }

        public Trading(int buysellthreshold, int smaTimePeriod, int smaGranularity, int emaTimePeriod, int emaGranularity, string coinType)
        {
            BuySellThreshold = buysellthreshold;
            SmaTimePeriod = smaTimePeriod;
            SmaGranularity = smaGranularity;
            EmaTimePeriod = emaTimePeriod;
            EmaGranularity = emaGranularity;
            CoinType = coinType;
            CandleSticksStrategies.CoinType = coinType;
        }
        
        public string SMATripleCrossover(SMA smas)
        {
            //if(smas.Count < 3){ return null; }

            //get all the smas we need to look at and order the list in asc order
            //smas.OrderBy(x => x.TimePeriod);
            //List<double> calcedSma = new List<double>();

            var dub = CandleSticksStrategies.GetSlopes(smas.TimePeriod, smas.Granularity, CoinType).Result;

            return "";
        }

        public async Task<Tuple<double,double,double>> EmaSmaCrossover()
        {
            double sma = await CandleSticksStrategies.GetEMA(SmaTimePeriod, SmaGranularity);
            double ema = await CandleSticksStrategies.GetEMA(EmaTimePeriod, EmaGranularity);

            double buysellThreshold = Math.Abs(sma - ema);

            if (_lastEma == 0 && _lastSma == 0)
            {
                _lastSma = sma;
                _lastEma = ema;
            }

            //If this is our first run then set an sma/ema
            else if (buysellThreshold >= BuySellThreshold)
            {
                if (_lastSma <= _lastEma && sma > ema && SellCoin(100, ema, sma))
                {                    
                    _lastSma = sma;
                    _lastEma = ema;
                    return new Tuple<double, double, double>(_lastSma, _lastEma, funds);
                    
                }
                else if (_lastSma >= _lastEma && sma < ema && BuyCoin(100, ema, sma))
                {                    
                    _lastSma = sma;
                    _lastEma = ema;
                    return new Tuple<double, double, double>(_lastSma, _lastEma, funds);                    
                }               
            }          
           
            counter++;
            return new Tuple<double, double, double>(_lastSma, _lastEma, 0);
        }
         
        //Buy coin at current price
        public bool BuyCoin(double buyAmount, double ema, double sma)
        {
            var btcPrice = CandleSticksStrategies.CoinPrice;
            if(funds > 100)
            {
                funds -= buyAmount;

                btcHolding += buyAmount / btcPrice;

                using (StreamWriter w = File.AppendText(mydocpath))
                {
                    Log($"Ema/Sma: {SmaTimePeriod}" + "/" +
                        $"{EmaTimePeriod}\r\nBought BTC at: ${btcPrice}\r\nEma Price: {ema}\r\nSma Price: {sma}\r\nCurrent Funds: {funds}", w);
                }
                return true;
            }
            return false;
        }

        //Sell coin at current price
        public bool SellCoin(double sellAmount, double ema, double sma)
        {
            string transactionDescription = string.Empty;

            var btcPrice = CandleSticksStrategies.CoinPrice;
            var availableBtc = btcHolding;
            var btcSellAmount = sellAmount / btcPrice;

            if ((availableBtc - btcSellAmount) > 0)
            {
                btcHolding -= sellAmount / btcPrice;

                funds += btcPrice / sellAmount;

                using (StreamWriter w = File.AppendText(mydocpath))
                {
                    Log($"Coin: {CoinType}\r\nEma/Sma: {SmaTimePeriod}" + "/" +
                        $"{EmaTimePeriod}\r\nBuy Sell Threshold: {BuySellThreshold}\r\nSold BTC at: ${btcPrice}\r\nEma Price: {ema}\r\nSma Price: {sma}"
                        + $"\r\nCurrent Funds: {funds}", w);
                }
                return true;
            }
            return false;
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
