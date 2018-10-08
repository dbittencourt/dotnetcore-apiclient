using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using APIClient;
using Newtonsoft.Json;

namespace APIClient
{
    public class RestClient : IClient
    {
        public RestClient()
        {
            // this class is meant to be used as singleton injected through dependency injection
            _httpClient = new HttpClient();
            _endpoints = new HashSet<string>();

            // Default is 2 minutes: https://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.dnsrefreshtimeout(v=vs.110).aspx
            ServicePointManager.DnsRefreshTimeout = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;
            // Increases the concurrent outbound connections
            ServicePointManager.DefaultConnectionLimit = 1024;
        }

        /// <summary>
        /// Makes a rest api request
        /// </summary>
        /// <param name="address">URI address</param>
        /// <param name="optional">Optional request parameters (method, bearer token, etc)</param>
        /// <returns>Response content in string format</returns>
        public async Task<string> RequestAsync(string address, IDictionary<string, string> optional = null)
        {
            HttpResponseMessage response = await MakeRequest(address, optional);
            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }

        /// <summary>
        /// Makes a rest api request
        /// </summary>
        /// <param name="address">URI address</param>
        /// <param name="optional">Optional request parameters (method, bearer token, etc)</param>
        /// <returns>Response content in provided type</returns>
        public async Task<T> RequestAsync<T>(string address, IDictionary<string, string> optional = null,
            bool handleTypes = false) where T : class
        {
            HttpResponseMessage response = await MakeRequest(address, optional);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (handleTypes)
                return JsonConvert.DeserializeObject<T>(responseContent,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            else
                return JsonConvert.DeserializeObject<T>(responseContent);
        }

        private async Task<HttpResponseMessage> MakeRequest(string address, IDictionary<string, string> optional)
        {
            AddConnectionLeaseTimeout(address);

            if (optional == null)
                optional = new Dictionary<string, string>();

            // todo: refactor this to use an enum with possible optional parameters instead of strings
            var method = optional.ContainsKey("method") ? optional["method"] : "get";
            var req = new HttpRequestMessage(new HttpMethod(method), address);

            if (optional.ContainsKey("bearer"))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", optional["bearer"]);
            if (optional.ContainsKey("content"))
            {
                req.Content = new StringContent(optional["content"], Encoding.Default, 
                optional.ContainsKey("content-type") ? optional["content-type"] : "application/json");
            }

            var response = await _httpClient.SendAsync(req);

            if (response.IsSuccessStatusCode)
                return response;

            var responseContent = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                //todo: implement custom exceptions
                case HttpStatusCode.NotFound:
                case HttpStatusCode.BadRequest:
                default:
                    throw new Exception(responseContent);
            }
        }

        private void AddConnectionLeaseTimeout(string endpoint)
        {
            lock (_endpoints)
            {
                if (_endpoints.Contains(endpoint))
                    return;

                ServicePointManager.FindServicePoint(new Uri(endpoint)).ConnectionLeaseTimeout
                    = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

                _endpoints.Add(endpoint);
            }
        }

        private readonly HttpClient _httpClient;
        private readonly HashSet<string> _endpoints;
    }
}