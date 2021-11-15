using System;
using System.Collections.Generic;
using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Services
{
    public abstract class RenewalStoreSecondary : IRenewalStore
    {
        const string NotImplementedMessage = "Secondary renewal store is not master";
        public IEnumerable<Renewal> Renewals => throw new NotImplementedException(NotImplementedMessage);
        public abstract void Cancel(Renewal renewal);
        public abstract void Clear();
        public void Encrypt() => throw new NotImplementedException(NotImplementedMessage);
        public IEnumerable<Renewal> FindByArguments(string? id, string? friendlyName) => throw new NotImplementedException(NotImplementedMessage);
        public abstract void Import(Renewal renewal);
        public abstract void Save(Renewal renewal, RenewResult result);
    }
}
