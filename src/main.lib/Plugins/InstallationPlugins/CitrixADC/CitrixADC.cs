using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class CitrixADC : IInstallationPlugin
    {
        private readonly ILogService _log;
        private readonly CitrixADCOptions _options;
        private readonly CitrixAdcClient _client;

        public CitrixADC (ILogService log, CitrixADCOptions options, CitrixAdcClient adcClient)
        {
            _log = log;
            _options = options;
            _client = adcClient;
        }

        public (bool, string?) Disabled => (false, null);

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
        {
            var clearPassword = new ProtectedString(_options.NitroPass ?? "", _log).Value;
            _log.Information($"Installing {target.CommonName.Value} using Nitro API at {_options.NitroHost}.");
            await _client.UpdateCertificate(newCertificateInfo, target.CommonName.Value, _options.NitroHost, _options.NitroUser, clearPassword);
            return true;
        }
    }
}
