﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CertificateManager.Core.Extensions;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    /// <summary>
    /// Doesn't actually do the watching, just copies the cert from where the
    /// store plugin saves it to a network share where it is watched and reloaded.
    /// </summary>
    public class FileWatcher : IInstallationPlugin
    {
        public static string PfxFilePassword(ISettingsService settings) =>
            (settings.Installation?.DefaultTomcatPassword).IfBlank(settings.Store?.PemFiles?.DefaultPassword).IfBlank(CitrixAdcClient.DefaultPemFilesPassword)!;

        const string DefaultPfxFilePath = @"\\oit00pdcm001.dohsd.ad.state.fl.us\store";

        private readonly ILogService _log;
        private readonly FileWatcherOptions _options;
        private readonly string _pfxFilePath;
        //private readonly string _pfxFilePassword;

        public FileWatcher(FileWatcherOptions options, ILogService log, ISettingsService settings, ArgumentsParser arguments)
        {
            _log = log;
            _options = options;
            var pfxFilePath = arguments.GetArguments<PemFilesArguments>()?.PemFilesPath;
            if (string.IsNullOrWhiteSpace(pfxFilePath))
            {
                pfxFilePath = settings.Store.PemFiles?.DefaultPath;
            }
            _pfxFilePath = pfxFilePath ?? DefaultPfxFilePath;
            //_pfxFilePassword = PfxFilePassword(settings);
        }

        public (bool, string?) Disabled => (false, null);

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificateInfo, CertificateInfo? oldCertificateInfo)
        {
            var storedCertInfo = GetCertFileInfo(stores, newCertificateInfo);
            var destinationPath = Path.Combine(_options.Path!, storedCertInfo.Name);
            _log.Information($"File watcher install copying {storedCertInfo.Name} to {_options.Path}");
            storedCertInfo.CopyTo(destinationPath, true);
            Thread.Sleep(100);
            var success = new FileInfo(destinationPath).Exists;
            return await Task.Run(() => success);
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
