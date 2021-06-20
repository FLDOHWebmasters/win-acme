﻿using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFiles : IStorePlugin
    {
        public const string CertFilenameSuffix = "-crt";
        public const string ChainFilenameSuffix = "-chain";
        public const string ChainOnlyFilenameSuffix = "-chain-only";
        public const string KeyFilenameSuffix = "-key";
        public const string FilenameExtension = ".pem";

        private readonly ILogService _log;
        private readonly PemService _pemService;
        private readonly string _path;
        private readonly string? _password;

        public PemFiles(
            ILogService log, ISettingsService settings,
            PemService pemService, PemFilesOptions options,
            SecretServiceManager secretServiceManager)
        {
            _log = log;
            _pemService = pemService;

            var passwordRaw = !string.IsNullOrWhiteSpace(options.PemPassword?.Value) ?
                options.PemPassword.Value :
                settings.Store.PemFiles.DefaultPassword;
            _password = secretServiceManager.EvaluateSecret(passwordRaw);

            var path = options.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = settings.Store.PemFiles.DefaultPath;
            }
            if (!string.IsNullOrWhiteSpace(path) && path.ValidPath(log))
            {
                _log.Debug("Using .pem files path: {path}", path);
                _path = path;
            }
            else
            {
                throw new Exception($"Specified .pem files path {path} is not valid.");
            }
        }

        public async Task Save(CertificateInfo input)
        {
            _log.Information("Exporting .pem files to {folder}", _path);
            try
            {
                // Determine name
                var name = PfxFile.Filename(input.CommonName.Value);

                // Base certificate
                var certificateExport = input.Certificate.Export(X509ContentType.Cert);
                var certString = _pemService.GetPem("CERTIFICATE", certificateExport);
                var chainString = "";
                await File.WriteAllTextAsync(Path.Combine(_path, $"{name}{CertFilenameSuffix}{FilenameExtension}"), certString);

                // Rest of the chain
                foreach (var chainCertificate in input.Chain)
                {
                    // Do not include self-signed certificates, root certificates
                    // are supposed to be known already by the client.
                    if (chainCertificate.Subject != chainCertificate.Issuer)
                    {
                        var chainCertificateExport = chainCertificate.Export(X509ContentType.Cert);
                        chainString += _pemService.GetPem("CERTIFICATE", chainCertificateExport);
                    }
                }

                // Save complete chain
                await File.WriteAllTextAsync(Path.Combine(_path, $"{name}{ChainFilenameSuffix}{FilenameExtension}"), certString + chainString);
                await File.WriteAllTextAsync(Path.Combine(_path, $"{name}{ChainOnlyFilenameSuffix}{FilenameExtension}"), chainString);
                input.StoreInfo.TryAdd(
                    GetType(),
                    new StoreInfo()
                    {
                        Name = PemFilesOptions.PluginName,
                        Path = _path
                    });

                // Private key
                if (input.CacheFile != null)
                {
                    var pkPem = "";
                    var store = new Pkcs12Store(input.CacheFile.OpenRead(), input.CacheFilePassword?.ToCharArray());
                    var alias = store.Aliases.OfType<string>().FirstOrDefault(p => store.IsKeyEntry(p));
                    if (alias == null)
                    {
                        _log.Warning("No key entries found");
                        return;
                    }
                    var entry = store.GetKey(alias);
                    var key = entry.Key;
                    if (key.IsPrivate)
                    {
                        pkPem = _pemService.GetPem(entry.Key, _password);
                    }
                    if (!string.IsNullOrEmpty(pkPem))
                    {
                        await File.WriteAllTextAsync(Path.Combine(_path, $"{name}{KeyFilenameSuffix}{FilenameExtension}"), pkPem);
                    }
                    else
                    {
                        _log.Warning("No private key found in Pkcs12Store");
                    }
                } 
                else
                {
                    _log.Warning("No private key found in cache");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error exporting .pem files to folder");
            }
        }

        public Task Delete(CertificateInfo input) => Task.CompletedTask;

        public CertificateInfo? FindByThumbprint() => null;

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}
