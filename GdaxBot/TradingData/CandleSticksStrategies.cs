using GdaxBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace GdaxBot.TradingData
{
    class CandleSticksStrategies
    {
        private static readonly int[] validGranularities = { 60, 300, 900, 3600, 21600, 86400 };

        public double CoinPrice { get; set; }

        public string CoinType { get; set; }

        private bool CheckGranularity(int granularity)
        {
            foreach (var item in validGranularities)
            {
                if (item == granularity)
                {
                    return true;
                }
            }

            return false;
        }
        
        public async Task<List<CandleStick>> GetCandleStickData(int minutes, int granularity, string productId)
        {
            if (!CheckGranularity(granularity))
            {
                return null;
            }

            List<CandleStick> candleList = new List<CandleStick>();
            IEnumerable<object[]> candleDataList;
            string start = DateTime.UtcNow.AddMinutes(-minutes).ToString("o");
            string end = DateTime.UtcNow.ToString("o");

            try
            {
                APIConnect api = new APIConnect();

                var queryString = api.BuildQuery(new KeyValuePair<string, string>("start", start), new KeyValuePair<string, string>("end", end),
                                new KeyValuePair<string, string>("granularity", granularity.ToString()));
                var uriString = $"/products/{productId}/candles" + queryString;

                var response = await api.CallStringAsync(uriString);

                candleDataList = JsonConvert.DeserializeObject<IEnumerable<object[]>>(response);

                foreach (var item in candleDataList)
                {
                    candleList.Add(new CandleStick()
                    {
                        Time = Int32.Parse(item[0].ToString()),
                        Low = Convert.ToDouble(item[1].ToString()),
                        High = Convert.ToDouble(item[2].ToString()),
                        Open = Convert.ToDouble(item[3].ToString()),
                        Close = Convert.ToDouble(item[4].ToString()),
                        Volume = Convert.ToDouble(item[5].ToString())
                    });
                }
            }
            catch (HttpResponseException ex)
            {
                throw ex;
            }

            return candleList;
        }       

        public async Task<double> GetSlopes(int minutes, int granularity, string productId)
        {
            List<CandleStick> candles = new List<CandleStick>();
            double dataPointsOverTime = minutes * 60 / granularity;
            double slope = 0.0;

            var c = await GetCandleStickData(minutes, granularity, productId);

            for(int i = 0; i < dataPointsOverTime; i++)
            {
                candles.Add(c[i]);
            }

            for(int i = 0; i < dataPointsOverTime - 1; i++)
            {
                double y = candles[i].Close;
                double x = candles[i].Time / 60;
                double y1 = candles[i+1].Close;
                double x1 = candles[i+1].Time / 60;

                slope += (y1 - y) / (x1 - x);
            }

            var retSlope = slope / dataPointsOverTime;
            return retSlope;
        }

        public async Task<double> GetSMA(int minuteDifference, int granularity)
        {
            double sum = 0.0;
            double sma = 0.0;

            double dataPointsOverTime = minuteDifference * 60 / granularity;

            List<CandleStick> candles = await GetCandleStickData(minuteDifference, granularity, CoinType);

            if (candles.Count > 0)
            {
                CoinPrice = candles[0].Close;
                for (int i = 0; i < (int)dataPointsOverTime; i++)
                {
                    sum += candles[i].Close;
                }

                sma = sum / dataPointsOverTime;
            }
            return sma;
        }

        public async Task<double> GetEMA(int minuteDifference, int granularity)
        {
            double ema = 0.0;

            int emaDataPointsOverTime = minuteDifference * 60 / granularity;
            var multiplier = Math.Round((double)2 / (double)(emaDataPointsOverTime + 1), 4);

            List<CandleStick> emaCandles = await GetCandleStickData(minuteDifference, granularity, CoinType);

            if (emaCandles.Count > 0)
            {
                ema = emaCandles[emaDataPointsOverTime - 1].Close;

                for (int i = emaDataPointsOverTime - 2; i >= 0; i--)
                {
                    ema = (emaCandles[i].Close - ema) * multiplier + ema;
                }
            }
            return ema;
        }

        public async Task<Tuple<double, double>> GetMovingAverages(int smaMinuteDifference, int smaGraularity, int emaMinuteDifference, int emaGranularity)
        {
            double sum = 0.0;
            double ema = 0.0;
            double sma = 0.0;
            try
            {
                int smaDataPointsOverTime = smaMinuteDifference * 60 / smaGraularity;
                int emaDataPointsOverTime = emaMinuteDifference * 60 / emaGranularity;
                var multiplier = Math.Round((double)2 / (double)(emaDataPointsOverTime + 1), 4);

                List<CandleStick> smaCandles = await GetCandleStickData(smaMinuteDifference, smaGraularity, CoinType);
                List<CandleStick> emaCandles = await GetCandleStickData(emaMinuteDifference, emaGranularity, CoinType);

                CoinPrice = smaCandles[0].Close;

                for (int i = 0; i < smaDataPointsOverTime; i++)
                {
                    sum += smaCandles[i].Close;
                }

                sma = sum / smaDataPointsOverTime;

                //For a starting point on EMA we can either use an sma over the ema period to seed or use the current closing price
                //Using closing point of first data point
                ema = emaCandles[emaDataPointsOverTime - 1].Close;

                for (int i = emaDataPointsOverTime - 2; i >= 0; i--)
                {
                    ema = (emaCandles[i].Close - ema) * multiplier + ema;
                }
            }
            catch (Exception ex)
            {

            }

            return new Tuple<double, double>(sma, ema);
        }

    }
}
