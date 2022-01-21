using Autofac;
using Microsoft.Web.Administration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    public class IISClient : IISWebClient, IIISClient<IISSiteWrapper, IISBindingWrapper>
    {
        private List<IISSiteWrapper>? _ftpSites = null;

        public IISClient(ILogService log) : base(log) { }

        protected override void Reset() {
            base.Reset();
            _ftpSites = null;
        }

        #region _ Basic retrieval _

        IEnumerable<IIISSite> IIISClient.FtpSites => FtpSites;

        IIISSite IIISClient.GetFtpSite(long id) => GetFtpSite(id);

        public bool HasFtpSites => Version >= new Version(7, 5) && FtpSites.Any();

        public IEnumerable<IISSiteWrapper> FtpSites
        {
            get
            {
                if (ServerManager == null)
                {
                    return new List<IISSiteWrapper>();
                }
                if (_ftpSites == null)
                {
                    _ftpSites = ServerManager.Sites.AsEnumerable().
                        Where(s => s.Bindings.Any(sb => sb.Protocol == "ftp")).
                        OrderBy(s => s.Name).
                        Select(x => new IISSiteWrapper(x)).
                        ToList();
                }
                return _ftpSites;
            }
        }

        public IISSiteWrapper GetFtpSite(long id)
        {
            foreach (var site in FtpSites)
            {
                if (site.Site.Id == id)
                {
                    return site;
                }
            }
            throw new Exception($"Unable to find IIS SiteId #{id}");
        }

        #endregion

        #region _ Ftps Install _

        /// <summary>
        /// Update binding for FTPS site
        /// </summary>
        /// <param name="FtpSiteId"></param>
        /// <param name="newCertificate"></param>
        /// <param name="oldCertificate"></param>
        public void UpdateFtpSite(long FtpSiteId, CertificateInfo newCertificate, CertificateInfo? oldCertificate)
        {
            var ftpSites = FtpSites.ToList();
            var oldThumbprint = oldCertificate?.Certificate?.Thumbprint;
            var newThumbprint = newCertificate?.Certificate?.Thumbprint;
            var newStore = newCertificate?.StoreInfo[typeof(CertificateStore)].Path;
            var updated = 0;

            if (ServerManager == null)
            {
                return;
            }

            var sslElement = ServerManager.SiteDefaults.
                GetChildElement("ftpServer").
                GetChildElement("security").
                GetChildElement("ssl");
            if (RequireUpdate(sslElement, 0, FtpSiteId, oldThumbprint, newThumbprint, newStore))
            {
                sslElement.SetAttributeValue("serverCertHash", newThumbprint);
                sslElement.SetAttributeValue("serverCertStoreName", newStore);
                _log.Information(LogType.All, "Updating default ftp site setting");
                updated += 1;
            } 
            else
            {
                _log.Debug("No update needed for default ftp site settings");
            }

            foreach (var ftpSite in ftpSites)
            {
                sslElement = ftpSite.Site.
                    GetChildElement("ftpServer").
                    GetChildElement("security").
                    GetChildElement("ssl");

                if (RequireUpdate(sslElement, ftpSite.Id, FtpSiteId, oldThumbprint, newThumbprint, newStore))
                {
                    sslElement.SetAttributeValue("serverCertHash", newThumbprint);
                    sslElement.SetAttributeValue("serverCertStoreName", newStore);
                    _log.Information(LogType.All, "Updating ftp site {name}", ftpSite.Site.Name);
                    updated += 1;
                }
                else
                {
                    _log.Debug("No update needed for ftp site {name}", ftpSite.Site.Name);
                }
            }

            if (updated > 0)
            {
                _log.Information("Committing {count} {type} site changes to IIS", updated, "ftp");
                Commit();
            }
        }

        private static bool RequireUpdate(ConfigurationElement element, 
            long currentSiteId, long installSiteId, 
            string? oldThumbprint, string? newThumbprint,
            string? newStore)
        {
            var currentThumbprint = element.GetAttributeValue("serverCertHash").ToString();
            var currentStore = element.GetAttributeValue("serverCertStoreName").ToString();
            return currentSiteId == installSiteId
                ? !string.Equals(currentThumbprint, newThumbprint, StringComparison.CurrentCultureIgnoreCase) ||
                    !string.Equals(currentStore, newStore, StringComparison.CurrentCultureIgnoreCase)
                : string.Equals(currentThumbprint, oldThumbprint, StringComparison.CurrentCultureIgnoreCase);
        }

        #endregion
    }
}
