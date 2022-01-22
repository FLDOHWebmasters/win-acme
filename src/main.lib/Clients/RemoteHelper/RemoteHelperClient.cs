using CertificateManager.Core;
using Newtonsoft.Json;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    public class RemoteHelperClient
    {
        private readonly ILogService _log;
        private readonly string _host;

        public RemoteHelperClient(ILogService log, ArgumentsParser arguments)
            : this(log, arguments.GetArguments<RemoteHelperArguments>()?.Host!) { }

        private RemoteHelperClient(ILogService log, string host) { _log = log; _host = host; }

        public static bool Exists(string host, ILogService log) => new RemoteHelperClient(log, host).Exists();

        public bool Exists() => Get<string>("Ping").Result == "Pong";

        public static long? GetWebSite(string host, string name, ILogService log) => new RemoteHelperClient(log, host).GetWebSite(name);

        public long? GetWebSite(string name) => Get<long?>($"Target/{name}").Result;

        public async Task<InstallInfo> Install(InstallInfo info) => await Post<InstallInfo>("Install", JsonConvert.SerializeObject(info));

        private async Task<T> Get<T>(string route, string? jsonContent = null) => await Get<T>(_host, route, jsonContent);

        private async Task<T> Post<T>(string route, string? jsonContent = null) => await Post<T>(_host, route, jsonContent);

        private static async Task<T> Get<T>(string host, string route, string? jsonContent = null)
            => await Call<T>(host, route, HttpMethod.Get, jsonContent);

        private static async Task<T> Post<T>(string host, string route, string? jsonContent = null)
            => await Call<T>(host, route, HttpMethod.Post, jsonContent);

        public static async Task<T> Call<T>(string host, string route, HttpMethod method, string? jsonContent = null)
        {
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://{host}/{route}"),
                Method = method,
            };
            if (jsonContent != null)
            {
                request.Content = new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json);
            }
            using var response = await httpClient.SendAsync(request);
            var apiResponse = await response.Content.ReadAsStringAsync();
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(apiResponse, typeof(T));
            }
            return JsonConvert.DeserializeObject<T>(apiResponse);
        }
    }
}
