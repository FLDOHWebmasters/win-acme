using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Clients.CitrixADC
{
    internal class CitrixADCClient
    {
        public const string DefaultNitroHost = "172.21.22.240";
        public const string DefaultNitroUsername = "cert_tester";
        public const string DefaultNitroPasswordProtected = "nitro_v1_pass";
        public const string DefaultPemFilesPath = @"D:\CertificateManagement\store";
        public const string DefaultPemFilesPassword = "p@S5w0rD";

        private readonly ILogService _log;
        private readonly string _pemFilesPath;
        private readonly string _pemFilesPassword;

        public CitrixADCClient(ILogService log, ISettingsService settings, PemFilesOptions pemOptions)
        {
            _log = log;
            var pemFilesPath = pemOptions?.Path;
            if (string.IsNullOrWhiteSpace(pemFilesPath))
            {
                pemFilesPath = PemFiles.DefaultPath(settings);
            }
            _pemFilesPath = pemFilesPath ?? DefaultPemFilesPath;
            _pemFilesPassword = settings.Store.PemFiles?.DefaultPassword ?? DefaultPemFilesPassword;
        }

        public async Task UpdateCertificate(CertificateInfo input, string? site, string? host, string? username, string? password)
        {
            const string location = "/nsconfig/ssl";
            var pemFilesName = PemFiles.Filename(input);
            site ??= pemFilesName;
            host ??= DefaultNitroHost;
            username ??= DefaultNitroUsername;
            password ??= DefaultNitroPasswordProtected;
            var apiUrl = $"https://{host}/nitro/v1/config";

            // read the private key and cert chain files
            var keyPem = await FileBytes($"{pemFilesName}{PemFiles.KeyFilenameSuffix}{PemFiles.FilenameExtension}");
            var chainPem = await FileBytes($"{pemFilesName}{PemFiles.ChainFilenameSuffix}{PemFiles.FilenameExtension}");

            // initialize the HTTP client
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-NITRO-USER", username);
            httpClient.DefaultRequestHeaders.Add("X-NITRO-PASS", password);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // send the private key and cert chain files
            var keyFilename = await PostSystemFile(httpClient, apiUrl, location, site, keyPem, key: true);
            if (keyFilename == null) { throw new Exception($"Failed to post {site} key pem."); }
            var chainFilename = await PostSystemFile(httpClient, apiUrl, location, site, chainPem, key: false);
            if (chainFilename == null) { throw new Exception($"Failed to post {site} chain pem."); }

            // update the cert
            var success = await PostSSLCertKey(httpClient, apiUrl, site, chainFilename, keyFilename, _pemFilesPassword);
            if (!success) { throw new Exception($"Failed to update {site} certificate."); }

            // verify the cert was successfully updated
            var verified = await VerifySSLCertKey(httpClient, apiUrl, site, chainFilename, keyFilename);
            if (!verified) { throw new Exception($"Verification failed for {site} cert update."); }

            // delete the private key and cert chain files
            var deletedKey = await DeleteSystemFile(httpClient, apiUrl, location, keyFilename);
            if (!deletedKey) { _log.Warning("Failed to delete {keyFilename}.", keyFilename); }
            var deletedCert = await DeleteSystemFile(httpClient, apiUrl, location, chainFilename);
            if (!deletedCert) { _log.Warning("Failed to delete {chainFilename}.", chainFilename); }
        }

        private async Task<byte[]> FileBytes(string pemFilename)
        {
            var filePath = Path.Combine(_pemFilesPath, pemFilename);
            return await File.ReadAllBytesAsync(filePath);
        }

        private static async Task<bool> DeleteSystemFile(HttpClient client, string apiUrl, string location, string filename)
        {
            string escapedLocation = System.Web.HttpUtility.UrlEncode(location);
            using var response = await client.DeleteAsync($"{apiUrl}/systemfile/{filename}?args=filelocation:{escapedLocation}");
            var apiResponse = await response.Content.ReadAsStringAsync();
            var nitroResponse = JsonConvert.DeserializeObject<NitroResponse>(apiResponse);
            return response.StatusCode == HttpStatusCode.OK && nitroResponse.ErrorCode == 0;
        }

        private static async Task<string?> PostSystemFile(HttpClient client, string apiUrl, string location, string site, byte[] pem, bool key)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{apiUrl}/systemfile");
            const string encoding = "BASE64";
            if (!location.EndsWith("/")) { location += "/"; }
            var base64pem = Convert.ToBase64String(pem);
            var today = DateTime.Now.ToString("yyyyMMdd");
            var extension = key ? "key" : "cer";
            var filename = $"{site}.{today}.{extension}";
            // alternative to below: req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var payload = new { systemfile = new { filename = filename, filelocation = location, filecontent = base64pem, fileencoding = encoding } };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            return response.StatusCode == HttpStatusCode.Created ? filename : null;
            // status code 201 Created, no JSON response
            // status code 409 confict (if the file already exists)
            // { "errorcode": 1642, "message": "Cannot create output file. File already exists", "severity": "ERROR" }
        }

        private static async Task<bool> PostSSLCertKey(HttpClient client, string apiUrl, string site, string certFilename, string keyFilename, string passPlain)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{apiUrl}/sslcertkey");
            var payload = new { sslcertkey = new { certkey = site, cert = certFilename, key = keyFilename, passplain = passPlain } };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            return response.StatusCode == HttpStatusCode.OK;
            // status code 200 OK, no JSON response
            // status code 599 Netscaler specific error
            // { "errorcode": 1614, "message": "Invalid password", "severity": "ERROR" }
            // { "errorcode": 1647, "message": "Input file(s) not present or not accessible in current partition", "severity": "ERROR" }
        }

        private static async Task<bool> VerifySSLCertKey(HttpClient client, string apiUrl, string site, string certFilename, string keyFilename)
        {
            using var response = await client.GetAsync($"{apiUrl}/sslcertkey?filter=certkey:{site}");
            var apiResponse = await response.Content.ReadAsStringAsync();
            var sslCertKeyResponse = JsonConvert.DeserializeObject<SSLCertKeyResponse>(apiResponse);
            return response.StatusCode == HttpStatusCode.OK
                && sslCertKeyResponse?.SSLCertKey != null
                && sslCertKeyResponse.SSLCertKey.Any()
                && sslCertKeyResponse.SSLCertKey.First().CertKey == site
                && sslCertKeyResponse.SSLCertKey.First().Cert == certFilename
                && sslCertKeyResponse.SSLCertKey.First().Key == keyFilename;
        }

        public class NitroResponse
        {
            public int? ErrorCode { get; set; } // 0
            public string? Message { get; set; } // Done
            public string? Severity { get; set; } // NONE
        }

        public class SSLCertKeyResponse : NitroResponse
        {
            public IEnumerable<SSLCertKey>? SSLCertKey { get; set; }
        }

        public class SSLCertKey
        {
            public string? CertKey { get; set; } // dohtest.com
            public string? Cert { get; set; } // dohtest.com.20210517.cer
            public string? Key { get; set; } // dohtest.com.20210517.key
            public string? Inform { get; set; } // PEM
            public string? SignatureAlg { get; set; } // sha256WithRSAEncryption
            public IList<string>? CertificateType { get; set; } // [ "CLNT_CERT", "SRVR_CERT" ]
            public string? Serial { get; set; } // 04D36B3FF07D159B08A37E380A257F1CAF56
            public string? Issuer { get; set; } //  C=US,O=Let's Encrypt,CN=R3
            public string? ClientCertNotBefore { get; set; } // May 17 07:21:16 2021 GMT
            public string? ClientCertNotAfter { get; set; } // Aug 15 07:21:16 2021 GMT
            public int? DaysToExpiration { get; set; } // 80
            public string? Subject { get; set; } //  CN=dohtest.com
            public string? PublicKey { get; set; } // rsaEncryption
            public int? PublicKeySize { get; set; } // 3072
            public int? Version { get; set; } // 3
            public string? Priority { get; set; } // 0
            public string? Status { get; set; } // Valid
            public string? PassCrypt { get; set; } // 1686a37de77714cb42ee40ff417a2c1fafab9d6758fb153d629ed3a4e524451b
            public string? Data { get; set; } // 0
            public string? ServerName { get; set; } //  C=US,O=Let's Encrypt,CN=R3
            public string? ServiceName { get; set; } //  C=US,O=Let's Encrypt,CN=R3
            public string? ExpiryMonitor { get; set; } // ENABLED
            public string? NotificationPeriod { get; set; } // 30
            public string? OcspResponseStatus { get; set; } // NONE
        }
    }
}
