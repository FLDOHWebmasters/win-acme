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
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Clients
{
    public class CitrixAdcClient
    {
        public const string DefaultNitroHost = "172.21.22.240";
        public const string DefaultNitroUsername = "cert_tester";
        public const string DefaultNitroPasswordProtected = "enc-AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAAOstSmXydG0ymQXhTxf75vwQAAAACAAAAAAADZgAAwAAAABAAAACTqBoejv5xq9kb6z6Jq6jnAAAAAASAAACgAAAAEAAAANxtrybhVhhnhsDqUh8KkrUYAAAAJz/kwzNQ14BrgrECO94jb/DXEX7x2yCKFAAAAP/elU7n1e3MLUtO71bCajVCjw+6";
        public const string DefaultPemFilesPath = @"D:\CertificateManagement\store";
        public const string DefaultPemFilesPassword = "fo0b@rB42";

        private readonly ILogService _log;
        private readonly string _pemFilesPath;
        private readonly string _pemFilesPassword;
        private readonly bool _isDevelopmentEnvironment;

        public CitrixAdcClient(ILogService log, IEnvironment env, ISettingsService settings, IArgumentsParser arguments)
        {
            _log = log;
            var pemFilesPath = arguments.GetArguments<PemFilesArguments>()?.PemFilesPath;
            if (string.IsNullOrWhiteSpace(pemFilesPath))
            {
                pemFilesPath = settings.Store.PemFiles?.DefaultPath;
            }
            _pemFilesPath = pemFilesPath ?? DefaultPemFilesPath;
            _pemFilesPassword = settings.Store.PemFiles?.DefaultPassword ?? DefaultPemFilesPassword;
            _isDevelopmentEnvironment = env.IsDevelopment;
        }

        private static string GetApiUrl(string host) => $"https://{host}/nitro/v1/config";

        public async Task<string?> GetSummary(string host)
        {
            string? apiResponse = null;
            try
            {
                using var client = new HttpClient();
                var apiUrl = GetApiUrl(host) + "/lbvserver?view=summary";
                using var response = await client.GetAsync(apiUrl);
                apiResponse = await response.Content.ReadAsStringAsync();
            } catch (Exception x)
            {
                _log.Debug(x.ToString());
            }
            return string.IsNullOrWhiteSpace(apiResponse) ? null : apiResponse;
        }

        public async Task<SSLCertKey?> GetSSLCertKey(string host, string site)
        {
            using var client = new HttpClient();
            var apiUrl = GetApiUrl(host);
            var certKey = await GetSSLCertKey(client, apiUrl, site);
            return certKey;
        }

        public async Task UpdateCertificate(CertificateInfo input, string? site, string? host, string? username, string? password)
        {
            const string location = "/nsconfig/ssl";
            var pemFilesName = PfxFile.Filename(input.CommonName.Value, "");
            site ??= pemFilesName;
            host ??= DefaultNitroHost;
            username ??= DefaultNitroUsername;
            password ??= new ProtectedString(DefaultNitroPasswordProtected, _log).Value ?? "";
            _log.Verbose($"CitrixAdcClient UpdateCertificate site {site} host {host} creds {username}/{password}");
            var apiUrl = GetApiUrl(host);

            // in development, allow all certificates (self-signed, expired, etc.)
            using var handler = new HttpClientHandler();
            if (_isDevelopmentEnvironment)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            // initialize the HTTP client
            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("X-NITRO-USER", username);
            httpClient.DefaultRequestHeaders.Add("X-NITRO-PASS", password);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // send the private key and cert chain files
            var keyFilename = $"{pemFilesName}{PemFiles.KeyFilenameSuffix}{PemFiles.FilenameExtension}";
            keyFilename = await PostSystemFile(httpClient, apiUrl, location, site, keyFilename, key: true);
            if (keyFilename == null) { throw new Exception($"Failed to post {site} key pem."); }
            var chainFilename = $"{pemFilesName}{PemFiles.ChainFilenameSuffix}{PemFiles.FilenameExtension}";
            chainFilename = await PostSystemFile(httpClient, apiUrl, location, site, chainFilename, key: false);
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

        private static async Task<bool> DeleteSystemFile(HttpClient client, string apiUrl, string location, string filename)
        {
            var escapedLocation = System.Web.HttpUtility.UrlEncode(location);
            using var response = await client.DeleteAsync($"{apiUrl}/systemfile/{filename}?args=filelocation:{escapedLocation}");
            var apiResponse = await response.Content.ReadAsStringAsync();
            var nitroResponse = JsonConvert.DeserializeObject<NitroResponse>(apiResponse);
            return response.StatusCode == HttpStatusCode.OK && nitroResponse.ErrorCode == 0;
        }

        private async Task<string?> PostSystemFile(HttpClient client, string apiUrl, string filelocation, string site, string pemFilename, bool key)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{apiUrl}/systemfile");
            const string fileencoding = "BASE64";
            if (!filelocation.EndsWith("/")) { filelocation += "/"; }
            var filePath = Path.Combine(_pemFilesPath, pemFilename);
            var pem = await File.ReadAllBytesAsync(filePath);
            var filecontent = Convert.ToBase64String(pem);
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var extension = key ? "key" : "cer";
            var filename = $"{site}.{timestamp}.{extension}";
            // alternative to below: req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var payload = new { systemfile = new { filename, filelocation, filecontent, fileencoding } };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            var success = response.StatusCode == HttpStatusCode.Created;
            if (!success) { _log.Error($"CitrixAdcClient PostSystemFile {(int)response.StatusCode} {response.ReasonPhrase} {jsonPayload}"); }
            return success ? filename : null;
            // status code 201 Created, no JSON response
            // status code 409 confict (if the file already exists)
            // { "errorcode": 1642, "message": "Cannot create output file. File already exists", "severity": "ERROR" }
        }

        private async Task<bool> PostSSLCertKey(HttpClient client, string apiUrl, string certkey, string cert, string key, string passplain)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{apiUrl}/sslcertkey?action=update");
            var payload = new { sslcertkey = new { certkey, cert, key, passplain } };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            var success = response.StatusCode == HttpStatusCode.OK;
            if (!success) { _log.Error($"CitrixAdcClient PostSSLCertKey {(int)response.StatusCode} {response.ReasonPhrase} {jsonPayload}"); }
            return success;
            // status code 200 OK, no JSON response
            // status code 599 Netscaler specific error
            // { "errorcode": 1614, "message": "Invalid password", "severity": "ERROR" }
            // { "errorcode": 1647, "message": "Input file(s) not present or not accessible in current partition", "severity": "ERROR" }
        }

        private async Task<SSLCertKey?> GetSSLCertKey(HttpClient client, string apiUrl, string site)
        {
            using var response = await client.GetAsync($"{apiUrl}/sslcertkey?filter=certkey:{site}");
            var apiResponse = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<SSLCertKeyResponse>(apiResponse);
            var certKeys = jsonResponse.SSLCertKey;
            var success = response.StatusCode == HttpStatusCode.OK && certKeys != null && certKeys.Any();
            if (!success) { _log.Error($"CitrixAdcClient VerifySSLCertKey {certKeys?.Count()} {(int)response.StatusCode} {response.ReasonPhrase}"); }
            return success ? certKeys!.First() : null;
        }

        private async Task<bool> VerifySSLCertKey(HttpClient client, string apiUrl, string site, string certFilename, string keyFilename)
        {
            var certKey = await GetSSLCertKey(client, apiUrl, site);
            if (certKey == null) { return false; }
            var success = certKey.CertKey == site && certKey.Cert == certFilename && certKey.Key == keyFilename;
            if (!success) { _log.Information($"CitrixAdcClient VerifySSLCertKey {certKey.CertKey} {certKey.Cert} {certKey.Key}"); }
            return success;
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
