using Autofac;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace PKISharp.WACS.Clients.IIS
{
    public class IISWebClient : IIISWebClient<IISSiteWrapper, IISBindingWrapper>, IDisposable
    {
        public const string DefaultBindingPortFormat = "443";
        public const int DefaultBindingPort = 443;
        public const string DefaultBindingIp = "*";

        public Version Version { get; private set; }
        protected readonly ILogService _log;

        public IISWebClient(ILogService log)
        {
            _log = log;
            Version = GetIISVersion(_log);
        }

        private IServerManager? _serverManager;
        private List<IISSiteWrapper>? _webSites = null;

        protected virtual void Reset() => _webSites = null;

        /// <summary>
        /// Single reference to the ServerManager
        /// </summary>
        protected IServerManager? ServerManager => _serverManager ??= GetServerManager(Version, _log, _serverManager, NewServerManager, Reset);

        protected virtual IServerManager NewServerManager() => new ServerManagerWrapper();

        private static IServerManager? GetServerManager(Version version, ILogService log,
            IServerManager? serverManager = null, Func<IServerManager>? newServerManager = null, Action? then = null)
        {
            if (serverManager == null)
            {
                if (version.Major > 0)
                {
                    try
                    {
                        serverManager = newServerManager?.Invoke() ?? new ServerManagerWrapper();
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
        protected void Commit()
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
            Reset();
            if (_serverManager != null)
            {
                _serverManager.Dispose();
                _serverManager = null;
            }
        }

        IEnumerable<IIISSite> IIISWebClient.WebSites => WebSites;

        IIISSite IIISWebClient.GetWebSite(long id) => GetWebSite(id);

        public bool HasWebSites => Version.Major > 0 && WebSites.Any();

        public IEnumerable<IISSiteWrapper> WebSites => _webSites ??= GetWebSites(ServerManager, _log, _webSites, NewSiteWrapper);
        private IISSiteWrapper NewSiteWrapper(Site site) => new(site);
        private static List<IISSiteWrapper> GetWebSites(IServerManager? serverManager, ILogService log,
            List<IISSiteWrapper>? webSites = null, Func<Site, IISSiteWrapper>? newSiteWrapper = null)
        {
            if (serverManager == null)
            {
                return new List<IISSiteWrapper>();
            }
            if (webSites == null)
            {
                webSites = serverManager.Sites.
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
                            // application pools from crashing the whole app
                            log.Warning("Unable to determine state for Site {id}", s.Id);
                            // nonetheless treat it as started since we know no better
                            return true;
                        }
                    }).
                    OrderBy(s => s.Name).
                    Select(x => newSiteWrapper?.Invoke(x) ?? new IISSiteWrapper(x)).
                    ToList();
            }
            return webSites;
        }

        public virtual IISSiteWrapper GetWebSite(long id) => WebSites.FirstOrDefault(x => x.Id == id) ?? throw new Exception($"Unable to find IIS SiteId #{id}");
        public virtual long? GetWebSite(string name) => WebSites.FirstOrDefault(x => x.Name == name)?.Id ?? throw new Exception($"Unable to find IIS site '{name}'");

        public static IISSiteWrapper GetWebSite(long siteId, ILogService log)
        {
            log.Verbose($"Looking for IIS Site ID {siteId}");
            var site = GetWebSite(x => x.Site.Id == siteId, log);
            return site ?? throw new Exception($"Unable to find IIS Site ID {siteId}");
        }

        public static IISSiteWrapper GetWebSite(string name, ILogService log)
        {
            log.Verbose($"Looking for IIS Site name {name}");
            var site = GetWebSite(x => x.Site.Name == name, log);
            return site ?? throw new Exception($"Unable to find IIS Site name {name}");
        }

        private static IISSiteWrapper? GetWebSite(Func<IISSiteWrapper, bool> isMatch, ILogService log)
        {
            var version = GetIISVersion(log);
            var serverManager = GetServerManager(version, log);
            if (serverManager != null)
            {
                var webSites = GetWebSites(serverManager, log);
                log.Verbose($"Found {webSites.Count} websites");
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

        public virtual IIISBinding AddBinding(IISSiteWrapper site, BindingOptions options)
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

        public virtual void UpdateBinding(IISSiteWrapper site, IISBindingWrapper existingBinding, BindingOptions options)
        {
            // Replace instead of change binding because of #371
            var handled = new[] { "protocol", "bindingInformation", "sslFlags", "certificateStoreName", "certificateHash" };
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

        /// <summary>
        /// Determine IIS version based on registry
        /// </summary>
        /// <returns></returns>
        protected static Version GetIISVersion(ILogService log)
        {
            // Get the W3SVC service
            var iisService = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "W3SVC");
            if (iisService == null)
            {
                log.Verbose("W3SVC service not detected");
                return new Version(0, 0);
            }
            if (iisService.Status != ServiceControllerStatus.Running)
            {
                log.Verbose("W3SVC service not running");
                return new Version(0, 0);
            }
            try
            {
                var hive = Registry.LocalMachine;
                using var componentsKey = hive.OpenSubKey(@"Software\Microsoft\InetStp", false);
                if (componentsKey != null)
                {
                    _ = int.TryParse(componentsKey.GetValue("MajorVersion", "-1")?.ToString() ?? "-1", out var majorVersion);
                    _ = int.TryParse(componentsKey.GetValue("MinorVersion", "-1")?.ToString() ?? "-1", out var minorVersion);
                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                    log.Debug($"Invalid version {majorVersion} + {minorVersion}");
                }
                else
                {
                    log.Debug("InetStp registry key not found");
                }
            }
            catch (Exception ex)
            {
                log.Verbose("Error reading IIS version fomr registry: {message}", ex.Message);
            }
            log.Verbose("Unable to detect IIS version, making assumption");
            return new Version(10, 0);
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
