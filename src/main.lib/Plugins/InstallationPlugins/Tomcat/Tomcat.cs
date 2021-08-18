using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    /// <summary>
    /// Requisites:<list type="bullet">
    /// <item>There must be a "cascadecerts" share on the installation target machine at UNC path \\target-machine-name\cascadecerts
    /// which maps to the constant HostCertPath defined in this class, currently D:\cascadecerts</item>
    /// <item>The service account sa/certinstaller must have admin privileges on the installation target machine and
    /// write privileges on the cascadecerts share</item>
    /// <item>CATALINA_HOME environment variable must be set to root of tomcat installation (under current Cascade root)</item>
    /// <item>In tomcat\conf\server.xml there must be an HTTPS connector already defined, with its keystoreFile attribute
    /// set to a file path in the defined HostCertPath folder. This plugin naively looks for the string
    /// 'keystoreFile="D:\cascadecerts\' to find and replace the certificate file path with the new cert, and
    /// then looks for the next occurrence of 'keystorePass="' to find and replace the certificate password.</item>
    /// </list>
    /// </summary>
    public class Tomcat : IInstallationPlugin
    {
        const string HostCertDir = "cascadecerts";
        const string HostCertPath = @"D:\" + HostCertDir;
        const string DefaultPfxFilePath = @"\\oit00pdcm001.dohsd.ad.state.fl.us\store";
        const string DefaultPfxFilePassword = "fo0b@rB42";

        private readonly ILogService _log;
        private readonly ISystemManagementClient _system;
        private readonly TomcatOptions _options;
        private readonly string _pfxFilePath;
        private readonly string _pfxFilePassword;

        public Tomcat(TomcatOptions options, ISystemManagementClient system,
            ILogService log, ISettingsService settings, ArgumentsParser arguments)
        {
            _log = log;
            _system = system;
            _options = options;
            var pfxFilePath = arguments.GetArguments<PemFilesArguments>()?.PemFilesPath;
            if (string.IsNullOrWhiteSpace(pfxFilePath))
            {
                pfxFilePath = settings.Store.PemFiles?.DefaultPath;
            }
            _pfxFilePath = pfxFilePath ?? DefaultPfxFilePath;
            _pfxFilePassword = settings.Store.PemFiles?.DefaultPassword ?? DefaultPfxFilePassword;
        }

        public (bool, string?) Disabled => (false, null);

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
                => await Task.Run(() => Install(target, stores, newCertificateInfo));

        private bool Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo input)
        {
            var hostName = _options.HostName!;
            _log.Information($"Installing {target.CommonName.Value} for Tomcat on {hostName}");

            // 0. determine local and remote paths
            var storedCertFile = GetCertFileInfo(stores, input);
            var storedCertPath = Path.Combine(_pfxFilePath, storedCertFile.Name);
            var serverFileName = Path.GetFileNameWithoutExtension(storedCertFile.Name);
            var targetFileName = $"{serverFileName}_{DateTime.Now.ToFileTime()}{storedCertFile.Extension}";
            var serverFilePath = Path.Combine(HostCertPath, targetFileName);

            // 1. send cert file to remote machine
            var serverShareDir = @$"\\{hostName}\{HostCertDir}";
            var serverSharePath = Path.Combine(serverShareDir, targetFileName);
            if (!File.Exists(storedCertPath))
            {
                throw new ApplicationException($"Stored cert not found at {storedCertPath}");
            }
            File.Copy(storedCertPath, serverSharePath);
            //var commandLine = @$"copy ""{storedCertPath}"" ""{serverFilePath}"" /y";
            //_system.ExecuteCommandLine(hostName, commandLine);

            // 2. add certificate to Java keystore
            var commandLine = @$"""%CATALINA_HOME%\..\java\jdk\bin\keytool.exe"" -importkeystore -srckeystore ""{serverFilePath}"" -srcstoretype pkcs12 -destkeystore clientcert.jks -deststoretype JKS -storepass changeit";
            _system.ExecuteCommandLine(hostName, commandLine);

            // 3. update tomcat/conf/server.xml keystoreFile value
            const string configFileName = "server.xml";
            const string fileAttribute = "keystoreFile=\"";
            const string passAttribute = "keystorePass=\"";
            var targetXmlPath = @$"%CATALINA_HOME%\conf\{configFileName}";
            var serverXmlPath = Path.Combine(HostCertPath, configFileName);
            commandLine = @$"copy ""{targetXmlPath}"" ""{serverXmlPath}"" /y";
            _system.ExecuteCommandLine(hostName, commandLine);
            var sourceXmlPath = Path.Combine(serverShareDir, configFileName);
            var storedXmlPath = Path.Combine(_pfxFilePath, configFileName);
            File.Copy(sourceXmlPath, storedXmlPath, true);
            var serverXml = File.ReadAllText(storedXmlPath);
            var fileIndex = serverXml.IndexOf($"{fileAttribute}{HostCertPath}");
            var passIndex = serverXml.IndexOf(passAttribute, fileIndex);
            if (fileIndex < 0 || passIndex < 0)
            {
                throw new ApplicationException($"Unable to find {fileAttribute} or {passAttribute} in {configFileName}");
            }
            fileIndex += fileAttribute.Length;
            passIndex += passAttribute.Length;
            var fileEndIndex = serverXml.IndexOf('\"', fileIndex);
            var passEndIndex = serverXml.IndexOf('\"', passIndex);
            serverXml = $"{serverXml[..fileIndex]}{serverFilePath}{serverXml[fileEndIndex..passIndex]}{_pfxFilePassword}{serverXml[passEndIndex..]}";
            File.WriteAllText(storedXmlPath, serverXml);
            File.Copy(storedXmlPath, sourceXmlPath, true);
            commandLine = @$"copy ""{serverXmlPath}"" ""{targetXmlPath}"" /y";
            _system.ExecuteCommandLine(hostName, commandLine);

            // 4. restart Cascade
            var sc = new ServiceController("Cascade CMS", hostName);
            var success = !string.IsNullOrEmpty(sc.MachineName);
            if (success)
            {
                sc.Stop();
                sc.Start();
            }
            return success;
        }

        private FileInfo GetCertFileInfo(IEnumerable<IStorePlugin> stores, CertificateInfo input)
        {
            FileInfo? localCertFile = null;
            var pfxStore = stores.FirstOrDefault(x => x is PfxFile);
            if (pfxStore != null)
            {
                var certPath = input.StoreInfo[typeof(PfxFile)].Path;
                if (!string.IsNullOrEmpty(certPath))
                {
                    var pfxFileName = PfxFile.Filename(input.CommonName.Value);
                    var pfxFilePath = Path.Combine(_pfxFilePath, pfxFileName);
                    if (File.Exists(pfxFilePath))
                    {
                        localCertFile = new FileInfo(pfxFilePath);
                    }
                }
            }
            return localCertFile ?? throw new ArgumentException("PFX cert file missing", nameof(input));
        }
    }
}
