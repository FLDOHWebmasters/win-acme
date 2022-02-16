using CertificateManager.Core;
using Newtonsoft.Json;
using PKISharp.WACS.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    public class RemoteHelperClient
    {
        private readonly ILogService _log;

        public RemoteHelperClient(ILogService log) => _log = log;

        public static bool Exists(string host, ILogService log) => new RemoteHelperClient(log).Exists(host);

        public bool Exists(string host) => Get<string>(host, "Ping/", _log).Result == "Pong";

        public static long? GetWebSite(string host, string name, ILogService log) => new RemoteHelperClient(log).GetWebSite(host, name);

        public long? GetWebSite(string host, string name) => Get<long>(host, $"Target/{name}/", _log).Result;

        public async Task<InstallInfo?> Install(string host, InstallInfo info) =>
            await Post<InstallInfo>(host, "Install/", _log, JsonConvert.SerializeObject(info));

        private static async Task<T?> Get<T>(string host, string route, ILogService log, string? jsonContent = null)
            => await Call<T>(host, route, HttpMethod.Get, log, jsonContent);

        private static async Task<T?> Post<T>(string host, string route, ILogService log, string? jsonContent = null)
            => await Call<T>(host, route, HttpMethod.Post, log, jsonContent);

        public static async Task<T?> Call<T>(string host, string route, HttpMethod method, ILogService log, string? jsonContent = null)
        {
            const string contentType = MediaTypeNames.Application.Json;
            var uri = $"https://{host}/{route}";
            log.Verbose($"Calling {method} {uri} with payload {jsonContent}");
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = method,
                Content = jsonContent == null ? null : new StringContent(jsonContent, Encoding.UTF8, contentType),
            };
            request.Headers.Add("Accept", MediaTypeNames.Application.Json);
            using var response = await httpClient.SendAsync(request);
            var apiResponse = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"HTTP {response.StatusCode}: {apiResponse}");
            }
            return JsonConvert.DeserializeObject<T>(apiResponse);
        }
    }
}
