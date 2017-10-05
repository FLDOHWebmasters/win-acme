﻿using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class IISClient : Plugin
    {
        public Version Version = GetIISVersion();
        public IdnMapping IdnMapping = new IdnMapping();
        public const string PluginName = "IIS";
        public override string Name => PluginName;
        public enum SSLFlags
        {
            SNI = 1,
            CentralSSL = 2
        }

        public ServerManager GetServerManager()
        {
            if (_ServerManager == null)
            {
                if (Version.Major > 0)
                {
                    _ServerManager = new ServerManager();
                }
            }
            return _ServerManager;
        }
        private ServerManager _ServerManager;

        internal void UnlockSection(string path)
        {
            // Unlock handler section
            var config = GetServerManager().GetApplicationHostConfiguration();
            var section = config.GetSection(path);
            if (section.OverrideModeEffective == OverrideMode.Deny)
            {
                section.OverrideMode = OverrideMode.Allow;
                GetServerManager().CommitChanges();
                Program.Log.Warning("Unlocked section {section}", path);
            }
        }

        /// <summary>
        /// Install for regular bindings
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pfxFilename"></param>
        /// <param name="store"></param>
        /// <param name="certificate"></param>
        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 newCertificate, X509Certificate2 oldCertificate)
        {
            SSLFlags flags = 0;
            if (Version.Major >= 8) {
                flags = SSLFlags.SNI;
            }
            AddOrUpdateBindings(target, flags, newCertificate.GetCertHash(), oldCertificate?.GetCertHash(), store.Name);
        }

        /// <summary>
        /// Install for Central SSL bindings
        /// </summary>
        /// <param name="target"></param>
        public override void Install(Target target)
        {
            if (Version.Major < 8) {
                var errorMessage = "Centralized SSL is only supported on IIS8+";
                Program.Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            AddOrUpdateBindings(target, SSLFlags.CentralSSL | SSLFlags.SNI, null, null, null);
        }

        /// <summary>
        /// Update/create bindings for all host names in the certificate
        /// </summary>
        /// <param name="target"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        public void AddOrUpdateBindings(Target target, SSLFlags flags, byte[] newThumbprint, byte[] oldThumbprint, string store)
        {
            try
            {
                var targetSite = GetSite(target);
                IEnumerable<string> todo = target.GetHosts(true);
                var found = new List<string>();

                if (oldThumbprint != null)
                {
                    var siteBindings = GetServerManager().Sites.
                        SelectMany(site => targetSite.Bindings, (site, binding) => new { site, binding }).
                        Where(sb => sb.binding.Protocol == "https").
                        Where(sb => sb.site.Id != targetSite.Id).
                        Where(sb => StructuralComparisons.StructuralEqualityComparer.Equals(sb.binding.CertificateHash, oldThumbprint));

                    // Out-of-target bindings created using the old certificate, so let's 
                    // assume the user wants to update them and not create new bindings in
                    // the actual target site.
                    foreach (var sb in siteBindings)
                    {
                        try
                        {
                            UpdateBinding(sb.site, sb.binding, flags, newThumbprint, store);
                            found.Add(sb.binding.Host.ToLower());
                        }
                        catch (Exception ex)
                        {
                            Program.Log.Error(ex, "Error updating binding {host}", sb.binding.BindingInformation);
                            throw;
                        }
                    }
                }
 
                // We are left with bindings that have no https equivalent in any site yet
                // so we will create them in the orginal target site
                foreach (var host in todo)
                {
                    try
                    {
                        AddOrUpdateBindings(targetSite, host, flags, newThumbprint, store, Program.Options.SSLPort, !found.Contains(host));
                    }
                    catch (Exception ex)
                    {
                        Program.Log.Error(ex, "Error creating binding {host}: {ex}", host, ex.Message);
                        throw;
                    }
                }
                Program.Log.Information("Committing binding changes to IIS");
                GetServerManager().CommitChanges();
                Program.Log.Information("IIS will serve the new certificates after the Application Pool IdleTimeout has been reached.");
            }
            catch (Exception ex)
            {
                Program.Log.Error(ex, "Error installing");
                throw;
            }
        }

        /// <summary>
        /// Create or update a single binding in a single site
        /// </summary>
        /// <param name="site"></param>
        /// <param name="host"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        /// <param name="newPort"></param>
        public void AddOrUpdateBindings(Site site, string host, SSLFlags flags, byte[] thumbprint, string store, int newPort = 443, bool allowCreate = true)
        {
            var existingBindings = site.Bindings.Where(x => string.Equals(x.Host, host, StringComparison.CurrentCultureIgnoreCase)).ToList();
            var existingHttpsBindings = existingBindings.Where(x => x.Protocol == "https").ToList();
            var existingHttpBindings = existingBindings.Where(x => x.Protocol == "http").ToList();
            var update = existingHttpsBindings.Any();
            if (update)
            {
                // Already on HTTPS, update those bindings to use the Let's Encrypt
                // certificate instead of the existing one. Note that this only happens
                // for the target website, if other websites have bindings using other
                // certificates, they will remain linked to the old ones.
                foreach (var existingBinding in existingHttpsBindings)
                {
                    UpdateBinding(site, existingBinding, flags, thumbprint, store);
                }
            }
            else if (allowCreate)
            {
                Program.Log.Information(true, "Adding new https binding");
                string IP = "*";
                if (existingHttpBindings.Any()) {
                    IP = GetIP(existingHttpBindings.First().EndPoint.ToString(), host);
                } else {
                    Program.Log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                }
                Binding newBinding = site.Bindings.CreateElement("binding");
                newBinding.Protocol = "https";
                newBinding.BindingInformation = $"{IP}:{newPort}:{host}";
                newBinding.CertificateStoreName = store;
                newBinding.CertificateHash = thumbprint;
                newBinding.SetAttributeValue("sslFlags", flags);
                site.Bindings.Add(newBinding);
            }
            else
            {
                Program.Log.Information("Binding not created");
            }
        }

        /// <summary>
        /// Update existing bindng
        /// </summary>
        /// <param name="site"></param>
        /// <param name="existingBinding"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        private void UpdateBinding(Site site, Binding existingBinding, SSLFlags flags, byte[] thumbprint, string store)
        {
            var currentFlags = int.Parse(existingBinding.GetAttributeValue("sslFlags").ToString());
            if (currentFlags == (int)flags &&
                StructuralComparisons.StructuralEqualityComparer.Equals(existingBinding.CertificateHash, thumbprint) &&
                string.Equals(existingBinding.CertificateStoreName, store, StringComparison.InvariantCultureIgnoreCase))
            {
                Program.Log.Verbose("No binding update needed");
            }
            else
            {
                Program.Log.Information(true, "Updating existing https binding {host}:{port}", existingBinding.Host, existingBinding.EndPoint.Port);

                // Replace instead of change binding because of #371
                var handled = new[] { "protocol", "bindingInformation", "sslFlags", "certificateStoreName", "certificateHash" };
                Binding replacement = site.Bindings.CreateElement("binding");
                replacement.Protocol = existingBinding.Protocol;
                replacement.BindingInformation = existingBinding.BindingInformation;
                replacement.CertificateStoreName = store;
                replacement.CertificateHash = thumbprint;
                foreach (ConfigurationAttribute attr in existingBinding.Attributes)
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
                        Program.Log.Warning("Unable to set attribute {name} on new binding: {ex}", attr.Name, ex.Message);
                    }
                }
                if (flags > 0 || existingBinding.Attributes["sslFlags"] != null)
                {
                    replacement.SetAttributeValue("sslFlags", flags);
                }
                site.Bindings.Remove(existingBinding);
                site.Bindings.Add(replacement);
            }
        }

        private static Version GetIISVersion()
        {
            using (RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (componentsKey != null)
                {
                    int majorVersion = (int)componentsKey.GetValue("MajorVersion", -1);
                    int minorVersion = (int)componentsKey.GetValue("MinorVersion", -1);
                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                }
                return new Version(0, 0);
            }
        }

        private Site GetSite(Target target)
        {
            foreach (var site in GetServerManager().Sites)
            {
                if (site.Id == target.SiteId) return site;
            }
            throw new Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }

        private string GetIP(string HTTPEndpoint, string host)
        {
            string IP = "*";
            string HTTPIP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'),
                (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

            if (Version.Major >= 8 && HTTPIP != "0.0.0.0")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\r\nWarning creating HTTPS Binding for {host}.");
                Console.ResetColor();
                Console.WriteLine(
                    "The HTTP binding is IP specific; the app can create it. However, if you have other HTTPS sites they will all get an invalid certificate error until you manually edit one of their HTTPS bindings.");
                Console.WriteLine("\r\nYou need to edit the binding, turn off SNI, click OK, edit it again, enable SNI and click OK. That should fix the error.");
                Console.WriteLine("\r\nOtherwise, manually create the HTTPS binding and rerun the application.");
                Console.WriteLine("\r\nYou can see https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/HTTPS-Binding-With-Specific-IP for more information.");
                Console.WriteLine(
                    "\r\nPress Y to acknowledge this and continue. Press any other key to stop installing the certificate");
                var response = Console.ReadKey(true);
                if (response.Key == ConsoleKey.Y)
                {
                    IP = HTTPIP;
                }
                else
                {
                    throw new Exception(
                        "HTTPS Binding not created due to HTTP binding having specific IP; Manually create the HTTPS binding and retry");
                }
            }
            else if (HTTPIP != "0.0.0.0")
            {
                IP = HTTPIP;
            }
            return IP;
        }

        internal Target UpdateWebRoot(Target saved, Target match)
        {
            // Update web root path
            if (!string.Equals(saved.WebRootPath, match.WebRootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                Program.Log.Warning("- Change WebRootPath from {old} to {new}", saved.WebRootPath, match.WebRootPath);
                saved.WebRootPath = match.WebRootPath;
            }
            return saved;
        }

        internal Target UpdateAlternativeNames(Target saved, Target match)
        {
            // Add/remove alternative names
            var addedNames = match.AlternativeNames.Except(saved.AlternativeNames).Except(saved.GetExcludedHosts());
            var removedNames = saved.AlternativeNames.Except(match.AlternativeNames);
            if (addedNames.Count() > 0)
            {
                Program.Log.Warning("- Added host(s): {names}", string.Join(", ", addedNames));
            }
            if (removedNames.Count() > 0)
            {
                Program.Log.Warning("- Removed host(s): {names}", string.Join(", ", removedNames));
            }
            saved.AlternativeNames = match.AlternativeNames;
            return saved;
        }
    }
}