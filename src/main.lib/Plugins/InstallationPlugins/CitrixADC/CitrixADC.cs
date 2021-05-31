using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PKISharp.WACS.Clients.CitrixADC;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class CitrixADC : IInstallationPlugin
    {
        private readonly ILogService _log;
        private readonly CitrixADCOptions _options;
        private readonly CitrixADCClient _client;

        public CitrixADC (ILogService log, CitrixADCOptions options, CitrixADCClient adcClient)
        {
            _log = log;
            _options = options;
            _client = adcClient;
        }

        public (bool, string?) Disabled => (false, null);

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
        {
            _log.Information($"Installing {target.CommonName.Value} using Nitro API at {_options.NitroHost}.");
            await _client.UpdateCertificate(newCertificateInfo, target.CommonName.Value, _options.NitroHost, _options.NitroUser, _options.NitroPass);
            return true;
        }
    }
}
