using CertificateManager.Core.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Management;
using System.Net;

namespace PKISharp.WACS.Clients
{
    public class WindowsManagementClient
    {
        private readonly string _computerName;
        private readonly ILogService _log;

        public WindowsManagementClient(ILogService log)
        {
            _computerName = Dns.GetHostEntry(Environment.MachineName).HostName; //fully qualified hostname
            _log = log;
        }

        public bool CreateTxtRecord(string zone, string hostName, string descriptiveText)
        {
            try
            {
                CreateTxtRecord(_computerName, zone, hostName, descriptiveText, _log);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return false;
            }
        }

        public static void CreateTxtRecord(string server, string zone, string hostName, string descriptiveText, ILogService? log = null)
        {
            log?.Information($"WindowsManagementClient.CreateTxtRecord({zone}, {hostName}, {descriptiveText})");
            const string methodName = "CreateInstanceFromPropertyData";
            const int ttlCacheSeconds = 150;
            ManagementScope scope = new(@"\\.\root\MicrosoftDNS");
            scope.Connect();
            ManagementClass cmiClass = new(scope, new ManagementPath("MicrosoftDNS_TXTType"), null);
            var inParams = cmiClass.GetMethodParameters(methodName);

            inParams["DnsServerName"] = server;
            inParams["ContainerName"] = zone;
            inParams["OwnerName"] = $"{hostName}.{zone}";
            inParams["DescriptiveText"] = descriptiveText;
            inParams["TTL"] = ttlCacheSeconds;

            cmiClass.InvokeMethod(methodName, inParams, null);
        }

        public bool DeleteTxtRecord(string zone, string hostName, string descriptiveText)
        {
            try
            {
                return DeleteTxtRecord(_computerName, zone, hostName, descriptiveText, _log);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return false;
            }
        }

        public static bool DeleteTxtRecord(string server, string zone, string hostName, string? descriptiveText = null, ILogService? log = null)
        {
            // TODO filter by descriptiveText may be necessary in future
            log?.Information($"WindowsManagementClient.DeleteTxtRecord(DnsServerName={server}, ContainerName={zone}, OwnerName={hostName}.{zone}, DescriptiveText={descriptiveText})");
            string mql = @$"
SELECT * FROM MicrosoftDNS_TXTType
WHERE DnsServerName = '{server}'
    AND ContainerName = '{zone}'
    AND OwnerName = '{hostName}.{zone}'
";
            if (descriptiveText.NotBlank())
            {
                mql += $"    AND DescriptiveText = '{descriptiveText}'";
            }
            var query = new ObjectQuery(mql);
            ManagementScope scope = new(@"\\.\root\MicrosoftDNS");
            var searcher = new ManagementObjectSearcher(scope, query);
            scope.Connect();
            var results = searcher.Get();
            foreach (ManagementObject result in results)
            {
                result.Delete();
                result.Dispose();
            }
            return results.Cast<ManagementObject>().Any();
        }

        //const string DefaultServiceAccountName = "sa.certinstaller";
        //const string DefaultServiceAccountPass = ProtectedString.EncryptedPrefix + "encrypted-password";

        [Obsolete("Can't get this to work with machines hosted at state data center")]
        public void ExecuteCommandLine(string hostName, string commandLine)
        {
            _log.Verbose($"WindowsManagementClient.ExecuteCommandLine {commandLine}");
            //string rawPass = new ProtectedString(DefaultServiceAccountPass, _log).Value!;
            //SecureString secPass = new NetworkCredential("", rawPass).SecurePassword;
            //var connOptions = new ConnectionOptions {
            //	EnablePrivileges = true,
            //	Impersonation = ImpersonationLevel.Impersonate,
            //	Username = DefaultServiceAccountName,
            //	SecurePassword = secPass,
            //};
            ManagementScope scope = new(@$"\\{hostName}\root\cimv2"); //, connOptions);
            ManagementPath path = new("Win32_Process");
            ManagementClass process = new(scope, path, new ObjectGetOptions());
            var result = process.InvokeMethod("Create", new[] { commandLine });
            var retVal = Convert.ToInt32(result);
            _log.Debug($"WindowsManagementClient.ExecuteCommandLine on {hostName} result {retVal}");
        }
    }
}
