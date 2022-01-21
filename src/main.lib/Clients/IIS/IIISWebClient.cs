using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Clients.IIS
{
    public interface IIISWebClient
    {
        void Refresh();
        bool HasWebSites { get; }
        Version Version { get; }
        IEnumerable<IIISSite> WebSites { get; }
        IIISSite GetWebSite(long id);

        void AddOrUpdateBindings(IEnumerable<Identifier> identifiers, BindingOptions bindingOptions, byte[]? oldThumbprint);
    }

    public interface IIISWebClient<TSite, TBinding> : IIISWebClient
        where TSite : IIISSite<TBinding>
        where TBinding : IIISBinding
    {
        IIISBinding AddBinding(TSite site, BindingOptions bindingOptions);
        void UpdateBinding(TSite site, TBinding binding, BindingOptions bindingOptions);
        new IEnumerable<TSite> WebSites { get; }
        new TSite GetWebSite(long id);
        TSite GetWebSite(string name);
    }
}
