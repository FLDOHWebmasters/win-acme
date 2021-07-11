using Autofac;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    public class IISClient : IIISClient<IISSiteWrapper, IISBindingWrapper>, IDisposable
    {
        public const string DefaultBindingPortFormat = "443"; 
        public const int DefaultBindingPort = 443;
        public const string DefaultBindingIp = "*";

        public Version Version { get; set; }
        [SuppressMessage("Code Quality", "IDE0069:Disposable fields should be disposed", Justification = "Actually is disposed")]
        private readonly ILogService _log;
        private readonly string _iisHost;
        private ServerManager? _serverManager;
        private List<IISSiteWrapper>? _webSites = null;
        private List<IISSiteWrapper>? _ftpSites = null;

        public IISClient(ILogService log, ArgumentsParser arguments)
        {
            _log = log;
            _iisHost = arguments.GetArguments<IISWebArguments>()?.IISHost ?? "";
            Version = GetIISVersion(_iisHost);
        }

        /// <summary>
        /// Single reference to the ServerManager
        /// </summary>
        private ServerManager? ServerManager => _serverManager ??= GetServerManager(_iisHost, Version, _log, _serverManager, () => _webSites = _ftpSites = null);

        private static ServerManager? GetServerManager(string iisHost, Version version, ILogService log, ServerManager? serverManager = null, Action? then = null)
        {
            if (serverManager == null)
            {
                if (version.Major > 0)
                {
                    try
                    {
                        var local = string.IsNullOrWhiteSpace(iisHost);
                        serverManager = local ? new ServerManager() : ServerManager.OpenRemote(iisHost);
                    }
                    catch
                    {
                        log.Error($"Unable to create an IIS ServerManager");
                    }
                    then?.Invoke();
                }
            }
            return serverManager;
        }

        /// <summary>
        /// Commit changes to server manager and remove the 
        /// reference to the cached version because it might
        /// be the cause of some bug to keep using the same
        /// ServerManager to commit multiple changes
        /// </summary>
        private void Commit()
        {
            if (_serverManager != null)
            {
                try
                {
                    _serverManager.CommitChanges();
                }
                finally
                {
                    // We will still set ServerManager to null
                    // so that at least a new one will be created
                    // for the next time
                    Refresh();
                }
            }
        }

        public void Refresh()
        {
            _webSites = null;
            _ftpSites = null;
            if (_serverManager != null)
            {
                _serverManager.Dispose();
                _serverManager = null;
            }
        }

        #region _ Basic retrieval _

        IEnumerable<IIISSite> IIISClient.WebSites => WebSites;

        IEnumerable<IIISSite> IIISClient.FtpSites => FtpSites;

        IIISSite IIISClient.GetWebSite(long id) => GetWebSite(id);

        IIISSite IIISClient.GetFtpSite(long id) => GetFtpSite(id);

        public bool HasWebSites => Version.Major > 0 && WebSites.Any();

        public bool HasFtpSites => Version >= new Version(7, 5) && FtpSites.Any();

        public IEnumerable<IISSiteWrapper> WebSites => _webSites ??= GetWebSites(ServerManager, _log, _webSites);
        private static List<IISSiteWrapper> GetWebSites(ServerManager? serverManager, ILogService log, List<IISSiteWrapper>? webSites = null)
        {
            if (serverManager == null)
            {
                return new List<IISSiteWrapper>();
            }
            if (webSites == null)
            {
                webSites = serverManager.Sites.AsEnumerable().
                    Where(s => s.Bindings.Any(sb => sb.Protocol is "http" or "https")).
                    Where(s =>
                    {

                        try
                        {
                            return s.State == ObjectState.Started;
                        }
                        catch
                        {
                               // Prevent COMExceptions such as misconfigured
                               // application pools from crashing the whole 
                               log.Warning("Unable to determine state for Site {id}", s.Id);
                               // nonetheless treat it as started since we know no better
                               return true;
                        }
                    }).
                    OrderBy(s => s.Name).
                    Select(x => new IISSiteWrapper(x)).
                    ToList();
            }
            return webSites;
        }

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

        public IISSiteWrapper GetWebSite(long id)
        {
            foreach (var site in WebSites)
            {
                if (site.Site.Id == id)
                {
                    return site;
                }
            }
            throw new Exception($"Unable to find IIS SiteId #{id}");
        }

        public static IISSiteWrapper GetWebSite(string server, long siteId, ILogService log)
        {
            log.Verbose($"Looking for IIS Site ID {siteId}");
            var site = GetWebSite(server, x => x.Site.Id == siteId, log);
            return site ?? throw new Exception($"Unable to find IIS Site ID {siteId}");
        }

        public static IISSiteWrapper GetWebSite(string server, string name, ILogService log)
        {
            log.Verbose($"Looking for IIS Site name {name}");
            var site = GetWebSite(server, x => x.Site.Name == name, log);
            return site ?? throw new Exception($"Unable to find IIS Site name {name}");
        }

        private static IISSiteWrapper? GetWebSite(string server, Func<IISSiteWrapper, bool> isMatch, ILogService log)
        {
            var version = GetIISVersion(server);
            var serverManager = GetServerManager(server, version, log);
            if (serverManager != null)
            {
                var webSites = GetWebSites(serverManager, log);
                foreach (var site in webSites)
                {
                    if (isMatch(site))
                    {
                        return site;
                    }
                }
            }
            return null;
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

        #region _ Https Install _

        public void AddOrUpdateBindings(IEnumerable<Identifier> identifiers, BindingOptions bindingOptions, byte[]? oldThumbprint)
        {
            var updater = new IISHttpBindingUpdater<IISSiteWrapper, IISBindingWrapper>(this, _log);
            var updated = updater.AddOrUpdateBindings(identifiers, bindingOptions, oldThumbprint);
            if (updated > 0)
            {
                _log.Information("Committing {count} {type} binding changes to IIS", updated, "https");
                Commit();
            }
            else
            {
                _log.Warning("No bindings have been changed");
            }
        }

        public IIISBinding AddBinding(IISSiteWrapper site, BindingOptions options)
        {
            var newBinding = site.Site.Bindings.CreateElement("binding");
            newBinding.BindingInformation = options.Binding;
            newBinding.CertificateStoreName = options.Store;
            newBinding.CertificateHash = options.Thumbprint;
            newBinding.Protocol = "https";
            if (options.Flags > 0)
            {
                newBinding.SetAttributeValue("sslFlags", options.Flags);
            }
            site.Site.Bindings.Add(newBinding);
            return new IISBindingWrapper(newBinding);
        }

        public void UpdateBinding(IISSiteWrapper site, IISBindingWrapper existingBinding, BindingOptions options)
        {
            // Replace instead of change binding because of #371
            var handled = new[] {
                "protocol",
                "bindingInformation",
                "sslFlags",
                "certificateStoreName",
                "certificateHash"
            };
            var replacement = site.Site.Bindings.CreateElement("binding");
            replacement.BindingInformation = existingBinding.BindingInformation;
            replacement.CertificateStoreName = options.Store;
            replacement.CertificateHash = options.Thumbprint;
            replacement.Protocol = existingBinding.Protocol;
            foreach (var attr in existingBinding.Binding.Attributes)
            {
                try
                {
                    if (!handled.Contains(attr.Name) && attr.Value != null)
                    {
                        replacement.SetAttributeValue(attr.Name, attr.Value);
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning("Unable to set attribute {name} on new binding: {ex}", attr.Name, ex.Message);
                }
            }

            if (options.Flags > 0)
            {
                replacement.SetAttributeValue("sslFlags", options.Flags);
            }
            site.Site.Bindings.Remove(existingBinding.Binding);
            site.Site.Bindings.Add(replacement);
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

        private bool RequireUpdate(ConfigurationElement element, 
            long currentSiteId, long installSiteId, 
            string? oldThumbprint, string? newThumbprint,
            string? newStore)
        {
            var currentThumbprint = element.GetAttributeValue("serverCertHash").ToString();
            var currentStore = element.GetAttributeValue("serverCertStoreName").ToString();
            bool update;
            if (currentSiteId == installSiteId)
            {
                update =
                    !string.Equals(currentThumbprint, newThumbprint, StringComparison.CurrentCultureIgnoreCase) ||
                    !string.Equals(currentStore, newStore, StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                update = string.Equals(currentThumbprint, oldThumbprint, StringComparison.CurrentCultureIgnoreCase);
            }
            return update;
        }

        #endregion

        /// <summary>
        /// Determine IIS version based on registry
        /// </summary>
        /// <returns></returns>
        private static Version GetIISVersion(string iisHost)
        {
            try
            {
                if (string.IsNullOrEmpty(iisHost))
                {
                    return getVersion(Registry.LocalMachine);
                }
                else
                {
                    using var remoteHive = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, iisHost);
                    return getVersion(remoteHive);
                }
                Version getVersion(RegistryKey hive)
                {
                    using var componentsKey = hive.OpenSubKey(@"Software\Microsoft\InetStp", false);
                    if (componentsKey != null)
                    {
                        _ = int.TryParse(componentsKey.GetValue("MajorVersion", "-1")?.ToString() ?? "-1", out var majorVersion);
                        _ = int.TryParse(componentsKey.GetValue("MinorVersion", "-1")?.ToString() ?? "-1", out var minorVersion);
                        if (majorVersion != -1 && minorVersion != -1)
                        {
                            return new Version(majorVersion, minorVersion);
                        }
                    }
                    return new Version(0, 0);
                }
            }
            catch
            {
                // Assume nu IIS if we're not able to open the registry key
            }
            return new Version(0, 0);
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_serverManager != null)
                    {
                        _serverManager.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion

    }
}