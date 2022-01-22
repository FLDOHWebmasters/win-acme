using CertificateManager.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class RemoteHelper : IInstallationPlugin
    {
        private readonly ILogService _log;
        private readonly RemoteHelperClient _client;

        public RemoteHelper(ILogService log, RemoteHelperClient client)
        {
            _log = log;
            _client = client;
        }

        public (bool, string?) Disabled => (false, null);

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores,
            CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
        {
            var ids = target.GetIdentifiers(true);
            var installInfo = new InstallInfo
            {
                BindingIP = ids.FirstOrDefault(x => x.Type == IdentifierType.IpAddress)?.Value,
                BindingPort = IISWebClient.DefaultBindingPort,
                Name = target.CommonName.Value,
                Password = newCertificateInfo.CacheFilePassword,
                PfxBytes = File.ReadAllBytes(newCertificateInfo.CacheFile!.FullName),
                Sans = string.Join(",", ids.Select(x => x.Value)),
                SiteID = target.Parts.FirstOrDefault(x => x.IIS)?.SiteId,
            };
            installInfo = await _client.Install(installInfo);
            return installInfo != null;
        }
    }
}
