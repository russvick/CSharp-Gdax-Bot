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

        public async Task<string> CallStringAsync(string uri)
        {
            try
            {

                _httpClient.DefaultRequestHeaders.Add("User-Agent", "GdaxClient");
                string uriApi = apiUri;
                if (_sandBox == false)
                {
                    uriApi = sandBoxApiUri;
                }
                var response = await _httpClient.GetStringAsync(uriApi + uri);
                return response;
            }
            catch(HttpResponseException ex)
            {

            }
            return null;
        }

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
