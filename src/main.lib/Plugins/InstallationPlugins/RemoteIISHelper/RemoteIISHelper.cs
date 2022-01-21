using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class RemoteIISHelper : IInstallationPlugin
    {
        public (bool, string?) Disabled => (false, null);

        public Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
            => throw new NotImplementedException();
    }
}
