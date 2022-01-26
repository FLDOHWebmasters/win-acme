﻿using CertificateManager.Core;
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
        private readonly RemoteHelperOptions _options;

        public RemoteHelper(ILogService log, RemoteHelperClient client, RemoteHelperOptions options)
        {
            _log = log;
            _client = client;
            _options = options;
        }

        public (bool, string?) Disabled => (false, null);

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores,
            CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
        {
            var installInfo = new InstallInfo
            {
                Name = target.CommonName.Value,
                Password = newCertificateInfo.CacheFilePassword,
                PfxBytes = File.ReadAllBytes(newCertificateInfo.CacheFile!.FullName),
                Sans = string.Join(",", target.GetIdentifiers(true).Select(x => x.Value)),
                Site = _options.InstallationSite,
                BindingIP = _options.NewBindingIp,
                BindingPort = _options.NewBindingPort,
            };
            installInfo = await _client.Install(_options.HelperHost!, installInfo);
            return installInfo != null;
        }
    }
}
