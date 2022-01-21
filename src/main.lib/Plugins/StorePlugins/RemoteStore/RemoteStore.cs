using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    public class RemoteStore : IStorePlugin
    {
        public (bool, string?) Disabled => (false, null);

        public Task Save(CertificateInfo certificateInfo)
            => throw new NotImplementedException();

        public Task Delete(CertificateInfo certificateInfo)
            => throw new NotImplementedException();
    }
}
