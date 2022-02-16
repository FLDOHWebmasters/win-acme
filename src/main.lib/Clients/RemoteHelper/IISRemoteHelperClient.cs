using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    public class IISRemoteHelperClient : IISWebClient
    {
        private readonly string _helperHost;

        public IISRemoteHelperClient(ILogService log, ArgumentsParser arguments) : base(log)
            => _helperHost = arguments.GetArguments<RemoteIISHelperArguments>()?.IISHost!;

        private IISRemoteHelperClient(ILogService log, string host) : base(log) => _helperHost = host;

        public static IISSiteWrapper GetWebSite(string host, long id, ILogService log) => new IISRemoteHelperClient(log, host).GetWebSite(id);

        public override IISSiteWrapper GetWebSite(long id) => Get<IISSiteWrapper>($"Target/{id}").Result
             ?? throw new Exception($"Unable to find IIS Site ID {id} on {_helperHost}");

        public override long? GetWebSite(string name) => Get<long?>($"Target/{name}").Result;

        public async Task<Target?> Generate() => await Post<Target>("Target");

        public override IIISBinding AddBinding(IISSiteWrapper site, BindingOptions bindingOptions)
            => Post<IISRemoteHelperBinding>($"Install/Binding/{bindingOptions.SiteId}").Result!;

        public override void UpdateBinding(IISSiteWrapper site, IISBindingWrapper binding, BindingOptions bindingOptions)
            => _ = Call<IISRemoteHelperBinding>($"Install/Binding/{bindingOptions.SiteId}", HttpMethod.Put).Result;

        public async Task<Version> GetIISVersion() => new(await Get<string>("Target/Version") ?? "0.0");

        private async Task<T?> Get<T>(string route, string? jsonContent = null) => await Call<T>(route, HttpMethod.Get, jsonContent);

        private async Task<T?> Post<T>(string route, string? jsonContent = null) => await Call<T>(route, HttpMethod.Post, jsonContent);

        private async Task<T?> Call<T>(string route, HttpMethod method, string? jsonContent = null)
            => await RemoteHelperClient.Call<T>(_helperHost, route, method, _log, jsonContent);

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
