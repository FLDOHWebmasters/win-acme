using System.Collections.Generic;
using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Services
{
    public class RenewalStoreDual : IRenewalStore
    {
        private readonly RenewalStore _primaryStore;
        private readonly RenewalStoreSecondary _secondaryStore;

        public RenewalStoreDual(RenewalStore primary, RenewalStoreSecondary secondary)
        {
            _primaryStore = primary;
            _secondaryStore = secondary;
        }

        public IEnumerable<Renewal> Renewals => _primaryStore.Renewals;

        public void Cancel(Renewal renewal)
        {
            _primaryStore.Cancel(renewal);
            _secondaryStore.Cancel(renewal);
        }

        public void Clear()
        {
            _primaryStore.Clear();
            _secondaryStore.Clear();
        }

        public void Encrypt() => _primaryStore.Encrypt();
        public IEnumerable<Renewal> FindByArguments(string? id, string? friendlyName) => _primaryStore.FindByArguments(id, friendlyName);

        public void Import(Renewal renewal)
        {
            _primaryStore.Import(renewal);
            _secondaryStore.Import(renewal);
        }

        public void Save(Renewal renewal, RenewResult result)
        {
            _primaryStore.Save(renewal, result);
            _secondaryStore.Save(renewal, result);
        }
    }
}
