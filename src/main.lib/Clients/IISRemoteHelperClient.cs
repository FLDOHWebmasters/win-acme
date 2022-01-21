using Microsoft.Web.Administration;
using Newtonsoft.Json;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.OrderPlugins;
using PKISharp.WACS.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    public class IISRemoteHelperClient : IISWebClient
    {
        private readonly string? _helperHost;

        public IISRemoteHelperClient(ILogService log, ArgumentsParser arguments) : base(log)
            => _helperHost = arguments.GetArguments<RemoteIISHelperArguments>()?.IISHost;

        private IISRemoteHelperClient(ILogService log, string host) : base(log) => _helperHost = host;

        public static bool Exists(string host, ILogService log) => new IISRemoteHelperClient(log, host).Exists();

        public bool Exists() => Get<string>("Ping").Result == "Pong";

        public static IISSiteWrapper GetWebSite(string host, long id, ILogService log) => new IISRemoteHelperClient(log, host).GetWebSite(id);

        public static IISSiteWrapper GetWebSite(string host, string name, ILogService log) => new IISRemoteHelperClient(log, host).GetWebSite(name);

        public override IISSiteWrapper GetWebSite(long id) => Get<IISSiteWrapper>($"Target/Site/{id}").Result
             ?? throw new Exception($"Unable to find IIS Site ID {id} on {_helperHost}");

        public override IISSiteWrapper GetWebSite(string name) => Get<IISSiteWrapper>($"Target/SiteName/{name}").Result
             ?? throw new Exception($"Unable to find IIS Site name {name} on {_helperHost}");

        public async Task<Target> Generate() => await Get<Target>($"Target/Generate");

        public override IIISBinding AddBinding(IISSiteWrapper site, BindingOptions bindingOptions)
            => Post<IISRemoteHelperBinding>($"Install/Binding/{bindingOptions.SiteId}").Result;

        public override void UpdateBinding(IISSiteWrapper site, IISBindingWrapper binding, BindingOptions bindingOptions)
            => _ = Call<IISRemoteHelperBinding>($"Install/Binding/{bindingOptions.SiteId}", HttpMethod.Put).Result;

        private async Task<Version> GetIISVersion() => new(await Get<string>("Target/Version"));

        private async Task<T> Get<T>(string route, string? jsonContent = null) => await Call<T>(route, HttpMethod.Get, jsonContent);

        private async Task<T> Post<T>(string route, string? jsonContent = null) => await Call<T>(route, HttpMethod.Post, jsonContent);

        private async Task<T> Call<T>(string route, HttpMethod method, string? jsonContent = null)
        {
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://{_helperHost}/{route}"),
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

        public class IISRemoteHelperBinding : IIISBinding
        {
            public IISRemoteHelperBinding() => Host = Protocol = CertificateStoreName = BindingInformation = string.Empty;
            public string Host { get; set; }
            public string Protocol { get; set; }
            public byte[]? CertificateHash { get; set; }
            public string CertificateStoreName { get; set; }
            public string BindingInformation { get; set; }
            public string? IP { get; set; }
            public SSLFlags SSLFlags { get; set; }
            public int Port { get; set; }
        }
    }
}
