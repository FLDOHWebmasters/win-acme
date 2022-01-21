using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Clients.IIS
{
    public interface IIISClient : IIISWebClient
    {
        IEnumerable<IIISSite> FtpSites { get; }
        bool HasFtpSites { get; }
        IIISSite GetFtpSite(long id);
        void UpdateFtpSite(long siteId, CertificateInfo newCertificate, CertificateInfo? oldCertificate);
    }

    public interface IIISClient<TSite, TBinding> : IIISClient, IIISWebClient<TSite, TBinding>
        where TSite : IIISSite<TBinding>
        where TBinding : IIISBinding
    {
        new IEnumerable<TSite> FtpSites { get; }
        new TSite GetFtpSite(long id);
    }

    public interface IIISSite
    {
        long Id { get; }
        string Name { get; }
        string Path { get; }
        IEnumerable<IIISBinding> Bindings { get; }
    }

    public interface IIISSite<TBinding> : IIISSite
        where TBinding : IIISBinding
    {
        new IEnumerable<TBinding> Bindings { get; }
    }

    public interface IIISBinding
    {
        string Host { get; }
        string Protocol { get; }
        byte[]? CertificateHash { get; }
        string CertificateStoreName { get; }
        string BindingInformation { get; }
        string? IP { get; }
        SSLFlags SSLFlags { get; }
        int Port { get; }
    }
}