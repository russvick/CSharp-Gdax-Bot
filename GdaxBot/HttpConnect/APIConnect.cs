using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Net;
using System.Web.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using GdaxBot.Models;
using System.Linq;

namespace GdaxBot
{
    class APIConnect
    {
        #region const
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string apiUri = "https://api.gdax.com";
        private const string sandBoxApiUri = "https://api-public.sandbox.gdax.com";

        #endregion

        #region vars
        private readonly string ApiKey;
        private readonly string Passphrase;
        private readonly string Secret;
        private readonly bool _sandBox;
        private double _coinPrice;
        private static readonly HttpClient _httpClient = new HttpClient();
        public double CoinPrice { get { return _coinPrice; } }
        #endregion

        #region ctor
        public APIConnect(bool sandBox = true)
        {
            ApiKey = ConfigurationManager.AppSettings["ApiKey"];
            Passphrase = ConfigurationManager.AppSettings["Passphrase"];
            Secret = ConfigurationManager.AppSettings["ApiSecret"];
            _sandBox = sandBox;
        }
        #endregion

        #region Helpers
        public string CreateHttpRequestMessage(string requestUri, string contentBody = "")
        {
            var baseUri = _sandBox ? sandBoxApiUri : apiUri;  
            
            var timeStamp = GetTimeStamp();
            var signedSignature = ComputeSignature(HttpMethod.Get, Secret, timeStamp, requestUri, contentBody);
            AddHeaders(signedSignature, timeStamp);
            try
            {
                var response = _httpClient.GetAsync(new Uri($"{baseUri}{requestUri}")).Result;
                response.EnsureSuccessStatusCode();
                //var releases = JArray.Parse(response);

                return "";
            }
            catch(HttpRequestException ex)
            {
                return null;
            }
        }

        private void AddHeaders(string signedSignature, double timeStamp)
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GDAXClient");
            _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", ApiKey);
            _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", timeStamp.ToString());
            _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signedSignature);
            _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", Passphrase);
        }

        private string ComputeSignature(HttpMethod httpMethod, string secret, double timestamp, string requestUri, string contentBody = "")
        {
            var convertedString = Convert.FromBase64String(secret);
            var prehash = timestamp.ToString() + httpMethod.ToString().ToUpper() + requestUri + contentBody;
            return HashString(prehash, convertedString).ToUpper();
        }

        private string HashString(string str, byte[] secret)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var hmaccsha = new HMACSHA256(secret))
            {
                return Convert.ToBase64String(hmaccsha.ComputeHash(bytes));
            }
        }

        private async Task<string> CallStringAsync(string uri)
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GdaxClient");
            var response = await _httpClient.GetStringAsync(uri);
            return response;
        }

        private bool CheckGranularity(int granularity)
        {
            int[] validGranularities = { 60, 300, 900, 3600, 21600, 86400 };

            foreach(var item in validGranularities)
            {
                if(item == granularity)
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
                var queryString = BuildQuery(new KeyValuePair<string, string>("start", start), new KeyValuePair<string, string>("end", end),
                                new KeyValuePair<string, string>("granularity", granularity.ToString()));
                var uriString = $"{apiUri}/products/{productId}/candles" + queryString;

                var response = await CallStringAsync(uriString);

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
            catch(HttpResponseException ex)
            {
                throw ex;
            }
            
            return candleList;
        }

        //Timestamp is in the request header and for the hash while trying to retrieve users account information
        public double GetTimeStamp()
        {
            String URL = "https://api.gdax.com/time";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.UserAgent = "GdaxClient";            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var encoding = ASCIIEncoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                dynamic timeDesel = JsonConvert.DeserializeObject(reader.ReadToEnd());
                return timeDesel.epoch;
            }           
        }

        public double GetSMA(int minuteDifference, int granularity)
        {
            double sum = 0.0;
            double sma = 0.0;
            
            double dataPointsOverTime = minuteDifference * 60 / granularity;

            List<CandleStick> candles = GetCandleStickData(minuteDifference, granularity, ProductId.BTCUSD).Result;

            if(candles.Count > 0)
            {
                _coinPrice = candles[0].Close;
                for (int i = 0; i < (int)dataPointsOverTime; i++)
                {
                    sum += candles[i].Close;
                }

                sma = sum / dataPointsOverTime;
            }
            return sma;
        }

        public async Task<Tuple<double,double>> GetMovingAverages(int smaMinuteDifference, int smaGraularity, int emaMinuteDifference, int emaGranularity)
        {
            double sum = 0.0;
            double ema = 0.0;
            double sma = 0.0;
            try
            {
                int smaDataPointsOverTime = smaMinuteDifference * 60 / smaGraularity;
                int emaDataPointsOverTime = emaMinuteDifference * 60 / emaGranularity;
                var multiplier = Math.Round((double)2 / (double)(emaDataPointsOverTime + 1),4);

                List<CandleStick> smaCandles = await GetCandleStickData(smaMinuteDifference, smaGraularity, ProductId.BTCUSD);
                List<CandleStick> emaCandles = await GetCandleStickData(emaMinuteDifference, emaGranularity, ProductId.BTCUSD);

                _coinPrice = smaCandles[0].Close;

                for (int i = 0; i < smaDataPointsOverTime; i++)
                {
                    sum += smaCandles[i].Close;
                }

                sma = sum / smaDataPointsOverTime;

                //For a starting point on EMA we can either use an sma over the ema period to seed or use the current closing price
                //Using closing point of first data point
                ema = emaCandles[emaDataPointsOverTime - 1].Close;
                
                for (int i = emaDataPointsOverTime-2; i >= 0; i--)
                {
                    ema = (emaCandles[i].Close - ema) * multiplier + ema;
                }
            }
            catch(Exception ex)
            {

            }

            return new Tuple<double,double>(sma,ema);
        }

        public double GetBTCPrice()
        {
            List<CandleStick> candles = GetCandleStickData(5,60,ProductId.BTCUSD).Result;
            
            return candles[0].Close;
        }

        public string BuildQuery(params KeyValuePair<string, string>[] queryParameters)
        {
            var queryString = new StringBuilder("?");

            foreach (var queryParameter in queryParameters)
            {
                if (queryParameter.Value != string.Empty)
                {
                    queryString.Append(queryParameter.Key.ToLower() + "=" + queryParameter.Value.ToLower() + "&");
                }
            }

            return queryString.ToString().TrimEnd('&');
        }
        #endregion
    }
}
