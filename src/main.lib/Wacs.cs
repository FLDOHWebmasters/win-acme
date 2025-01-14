﻿using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Context;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    public class Wacs
    {
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly ArgumentsParser _arguments;
        private readonly IRenewalStore _renewalStore;
        private readonly ISettingsService _settings;
        private readonly IComponentContext _container;
        private readonly MainArguments _args;
        private readonly RenewalManager _renewalManager;
        private readonly RenewalCreator _renewalCreator;
        //private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly IUserRoleService _userRoleService;
        private readonly TaskSchedulerService _taskScheduler;
        private readonly SecretServiceManager _secretServiceManager;

        public Wacs(
            IComponentContext container, 
            //IAutofacBuilder scopeBuilder,
            ExceptionHandler exceptionHandler,
            ILogService logService,
            ISettingsService settingsService,
            IUserRoleService userRoleService,
            TaskSchedulerService taskSchedulerService,
            SecretServiceManager secretServiceManager)
        {
            // Basic services
            _container = container;
            //_scopeBuilder = scopeBuilder;
            _exceptionHandler = exceptionHandler;
            _log = logService;
            _settings = settingsService;
            _userRoleService = userRoleService;
            _taskScheduler = taskSchedulerService;
            _secretServiceManager = secretServiceManager;

            if (!string.IsNullOrWhiteSpace(_settings.UI.TextEncoding))
            {
                try
                {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(_settings.UI.TextEncoding);
                }
                catch
                {
                    _log.Warning("Error setting text encoding to {name}", _settings.UI.TextEncoding);
                }
            }

            _arguments = _container.Resolve<ArgumentsParser>();
            _arguments.ShowCommandLine();
            _args = _arguments.GetArguments<MainArguments>()!;
            _input = _container.Resolve<IInputService>();
            _renewalStore = _container.Resolve<IRenewalStore>();

            var renewalExecutor = container.Resolve<RenewalExecutor>(
                new TypedParameter(typeof(IContainer), _container));
            _renewalCreator = container.Resolve<RenewalCreator>(
                new TypedParameter(typeof(IContainer), _container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor));
            _renewalManager = container.Resolve<RenewalManager>(
                new TypedParameter(typeof(IContainer), _container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor),
                new TypedParameter(typeof(RenewalCreator), _renewalCreator));
        }

        /// <summary>
        /// Main program
        /// </summary>
        public async Task<int> Start()
        {
            // Show informational message and start-up diagnostics
            await ShowBanner().ConfigureAwait(false);

            // Version display (handled by ShowBanner in constructor)
            if (_args.Version)
            {
                await CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return 0;
                }
            }

            // Help function
            if (_args.Help)
            {
                _arguments.ShowArguments();
                await CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return 0;
                }
            }

            // Main loop
            do
            {
                try
                {
                    if (_args.List)
                    {
                        await _renewalManager.ShowRenewalsUnattended();
                        await CloseDefault();
                    }
                    else if (_args.Cancel)
                    {
                        await _renewalManager.CancelRenewalsUnattended();
                        await CloseDefault();
                    }
                    else if (_args.Revoke)
                    {
                        await _renewalManager.RevokeCertificatesUnattended();
                        await CloseDefault();
                    }
                    else if (_args.Renew)
                    {
                        var runLevel = RunLevel.Unattended;
                        if (_args.Force)
                        {
                            runLevel |= RunLevel.ForceRenew | RunLevel.IgnoreCache;
                        }
                        await _renewalManager.CheckRenewals(runLevel);
                        await CloseDefault();
                    }
                    else if (!string.IsNullOrEmpty(_args.Target) || !string.IsNullOrEmpty(_args.Source))
                    {
                        await _renewalCreator.SetupRenewal(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.Encrypt)
                    {
                        await Encrypt(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.SetupTaskScheduler)
                    {
                        await _taskScheduler.CreateTaskScheduler(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else
                    {
                        await MainMenu();
                    }
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                    await CloseDefault();
                }
                if (!_args.CloseOnFinish)
                {
                    _args.Clear();
                    _exceptionHandler.ClearError();
                    _container.Resolve<IIISClient>().Refresh();
                }
            }
            while (!_args.CloseOnFinish);

            // Return control to the caller
            _log.Verbose("Exiting with status code {code}", _exceptionHandler.ExitCode);
            return _exceptionHandler.ExitCode;
        }

        /// <summary>
        /// Show banner
        /// </summary>
        private async Task ShowBanner()
        {
            Console.WriteLine();
            _log.Information(LogType.Screen, "A simple Windows ACMEv2 client (WACS)");
            _log.Information(LogType.Screen, "Software version {version} ({build}, {bitness})", VersionService.SoftwareVersion, VersionService.BuildType, VersionService.Bitness);
            _log.Information(LogType.Disk | LogType.Event, "Software version {version} ({build}, {bitness}) started", VersionService.SoftwareVersion, VersionService.BuildType, VersionService.Bitness);
            if (_args != null)
            {
                _log.Information("Connecting to {ACME}...", _settings.BaseUri);
                var client = _container.Resolve<AcmeClient>();
                await client.CheckNetwork().ConfigureAwait(false);
            }
            var iis = _container.Resolve<IIISClient>().Version;
            if (iis.Major > 0)
            {
                _log.Debug("IIS version {version}", iis);
            }
            else
            {
                _log.Debug("IIS not detected");
            }
            if (_userRoleService.IsAdmin)
            {
                _log.Debug("Running with administrator credentials");
            }
            else
            {
                _log.Information("Running without administrator credentials, some options disabled");
            }
            _taskScheduler.ConfirmTaskScheduler();
            _log.Information("Please report issues at {url}", "https://github.com/win-acme/win-acme");
            _log.Verbose("Unicode display test: Chinese/{chinese} Russian/{russian} Arab/{arab}", "語言", "язык", "لغة");
        }

        /// <summary>
        /// Present user with the option to close the program
        /// Useful to keep the console output visible when testing
        /// unattended commands
        /// </summary>
        private async Task CloseDefault() => _args.CloseOnFinish = !_args.Test || _args.CloseOnFinish || await _input.PromptYesNo("[--test] Quit?", true);

        /// <summary>
        /// Main user experience
        /// </summary>
        private async Task MainMenu()
        {
            var total = _renewalStore.Renewals.Count();
            var due = _renewalStore.Renewals.Count(x => x.IsDue());
            var error = _renewalStore.Renewals.Count(x => !x.History.LastOrDefault()?.Success ?? false);
            var (allowIIS, allowIISReason) = _userRoleService.AllowIIS;
            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(
                    () => _renewalCreator.SetupRenewal(RunLevel.Interactive | RunLevel.Simple), 
                    "Create certificate (default settings)", "N", 
                    @default: true),
                Choice.Create<Func<Task>>(
                    () => _renewalCreator.SetupRenewal(RunLevel.Interactive | RunLevel.Advanced),
                    "Create certificate (full options)", "M"),
                Choice.Create<Func<Task>>(
                    () => _renewalManager.CheckRenewals(RunLevel.Interactive),
                    $"Run renewals ({due} currently due)", "R",
                    color: due == 0 ? null : ConsoleColor.Yellow),
                Choice.Create<Func<Task>>(
                    () => _renewalManager.ManageRenewals(),
                    $"Manage renewals ({total} total{(error == 0 ? "" : $", {error} in error")})", "A",
                    color: error == 0 ? null : ConsoleColor.Red,
                    disabled: (total == 0, "No renewals have been created yet.")),
                Choice.Create<Func<Task>>(
                    () => ExtraMenu(), 
                    "More options...", "O"),
                Choice.Create<Func<Task>>(
                    () => { _args.CloseOnFinish = true; _args.Test = false; return Task.CompletedTask; }, 
                    "Quit", "Q")
            };
            var chosen = await _input.ChooseFromMenu("Please choose from the menu", options);
            await chosen.Invoke();
        }

        /// <summary>
        /// Less common options
        /// </summary>
        private async Task ExtraMenu()
        {
            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(
                    () => _secretServiceManager.ManageSecrets(),
                    $"Manage secrets", "S"),
                Choice.Create<Func<Task>>(
                    () => _taskScheduler.CreateTaskScheduler(RunLevel.Interactive | RunLevel.Advanced), 
                    "(Re)create scheduled task", "T", 
                    disabled: (!_userRoleService.AllowTaskScheduler, 
                    "Run as an administrator to allow access to the task scheduler.")),
                Choice.Create<Func<Task>>(
                    () => _container.Resolve<EmailClient>().Test(), 
                    "Test email notification", "E"),
                Choice.Create<Func<Task>>(
                    () => UpdateAccount(RunLevel.Interactive), 
                    "ACME account details", "A"),
                Choice.Create<Func<Task>>(
                    () => Encrypt(RunLevel.Interactive), 
                    "Encrypt/decrypt configuration", "M"),
                Choice.Create<Func<Task>>(
                    () => Task.CompletedTask, 
                    "Back", "Q",
                    @default: true)
            };
            var chosen = await _input.ChooseFromMenu("Please choose from the menu", options);
            await chosen.Invoke();
        }

        /// <summary>
        /// Encrypt/Decrypt all machine-dependent information
        /// </summary>
        private async Task Encrypt(RunLevel runLevel)
        {
            var userApproved = !runLevel.HasFlag(RunLevel.Interactive);
            var encryptConfig = _settings.Security.EncryptConfig;
            var settings = _container.Resolve<ISettingsService>();
            if (!userApproved)
            {
                _input.Show(null, "To move your installation of win-acme to another machine, you will want " +
                "to copy the data directory's files to the new machine. However, if you use the Encrypted Configuration option, your renewal " +
                "files contain protected data that is dependent on your local machine. You can " +
                "use this tools to temporarily unprotect your data before moving from the old machine. " +
                "The renewal files includes passwords for your certificates, other passwords/keys, and a key used " +
                "for signing requests for new certificates.");
                _input.CreateSpace();
                _input.Show(null, "To remove machine-dependent protections, use the following steps.");
                _input.Show(null, "  1. On your old machine, set the EncryptConfig setting to false");
                _input.Show(null, "  2. Run this option; all protected values will be unprotected.");
                _input.Show(null, "  3. Copy your data files to the new machine.");
                _input.Show(null, "  4. On the new machine, set the EncryptConfig setting to true");
                _input.Show(null, "  5. Run this option; all unprotected values will be saved with protection");
                _input.CreateSpace();
                _input.Show(null, $"Data directory: {settings.Client.ConfigurationPath}");
                _input.Show(null, $"Config directory: {new FileInfo(VersionService.ExePath).Directory?.FullName}\\wacsettings.json");
                _input.Show(null, $"Current EncryptConfig setting: {encryptConfig}");
                userApproved = await _input.PromptYesNo($"Save all renewal files {(encryptConfig ? "with" : "without")} encryption?", false);
            }
            if (userApproved)
            {
                _renewalStore.Encrypt(); //re-saves all renewals, forcing re-write of all protected strings decorated with [jsonConverter(typeOf(protectedStringConverter())]

                var accountManager = _container.Resolve<AccountManager>();
                accountManager.EncryptSigner(); //re-writes the signer file

                var certificateService = _container.Resolve<ICertificateService>();
                certificateService.Encrypt(); //re-saves all cached private keys

                var secretService = _container.Resolve<SecretServiceManager>();
                secretService.Encrypt(); //re-writes the secrets file

                _log.Information("Your files are re-saved with encryption turned {onoff}", encryptConfig ? "on" : "off");
            }
        }

        /// <summary>
        /// Check/update account information
        /// </summary>
        /// <param name="runLevel"></param>
        private async Task UpdateAccount(RunLevel runLevel)
        {
            var acmeClient = _container.Resolve<AcmeClient>();
            var acmeAccount = await acmeClient.GetAccount();
            if (acmeAccount == null)
            {
                throw new InvalidOperationException("Unable to initialize acmeAccount");
            }
            _input.CreateSpace();
            _input.Show("Account ID", acmeAccount.Payload.Id ?? "-");
            _input.Show("Account KID", acmeAccount.Kid ?? "-");
            _input.Show("Created", acmeAccount.Payload.CreatedAt);
            _input.Show("Initial IP", acmeAccount.Payload.InitialIp);
            _input.Show("Status", acmeAccount.Payload.Status);
            if (acmeAccount.Payload.Contact != null &&
                acmeAccount.Payload.Contact.Length > 0)
            {
                _input.Show("Contact(s)", string.Join(", ", acmeAccount.Payload.Contact));
            }
            else
            {
                _input.Show("Contact(s)", "(none)");
            }
            if (await _input.PromptYesNo("Modify contacts?", false))
            {
                try
                {
                    await acmeClient.ChangeContacts();
                    await UpdateAccount(runLevel);
                } 
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }
        }

        class Env : IEnvironment
        {
            public bool IsDevelopment { get; }
            public Env(bool isDev) => IsDevelopment = isDev;
        }

        /// <summary>
        /// Configure dependency injection 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static void GlobalScope(ContainerBuilder builder, string[] args)
        {
            var isVerbose = args.Contains("--verbose");
            var isDev = args.Contains("--dev");
            var env = new Env(isDev);
            if (isDev)
            {
                args = args.Where(x => x != "--dev").ToArray();
            }
            var logger = new LogService();
            if (isVerbose)
            {
                logger.SetVerbose();
            }
            _ = new VersionService(logger);
            var pluginService = new PluginService(logger);
            var argumentsParser = new ArgumentsParser(logger, pluginService, args);
            if (!argumentsParser.Validate())
            {
                throw new Exception("Invalid arguments");
            }
            var mainArguments = argumentsParser.GetArguments<MainArguments>();
            if (mainArguments == null)
            {
                throw new Exception("Invalid main arguments");
            }
            var settingsService = new SettingsService(logger, mainArguments);
            if (!settingsService.Valid)
            {
                throw new Exception("Invalid settings");
            }
            logger.SetDiskLoggingPath(settingsService.Client.LogPath!);

            _ = builder.RegisterInstance(argumentsParser);
            _ = builder.RegisterInstance(mainArguments);
            _ = builder.RegisterInstance(logger).As<ILogService>();
            _ = builder.RegisterInstance(env).As<IEnvironment>();
            _ = builder.RegisterInstance(settingsService).As<ISettingsService>();
            _ = builder.RegisterInstance(pluginService).As<IPluginService>();
            _ = builder.RegisterType<UserRoleService>().As<IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<NoInputService>().As<IInputService>().SingleInstance();
            _ = builder.RegisterType<ProxyService>().As<IProxyService>().SingleInstance();
            //_ = builder.RegisterType<UpdateClient>().SingleInstance();
            _ = builder.RegisterType<PasswordGenerator>().SingleInstance();
            //_ = builder.RegisterType<RenewalStoreDisk>().As<RenewalStore>().SingleInstance();
            //_ = builder.RegisterType<RenewalStoreDatabase>().As<RenewalStoreSecondary>().SingleInstance();
            //_ = builder.RegisterType<RenewalStoreDual>().As<IRenewalStore>().SingleInstance();
            _ = builder.RegisterType<RenewalStoreDisk>().As<IRenewalStore>().SingleInstance();

            pluginService.Configure(builder);

            _ = builder.RegisterType<DomainParseService>().SingleInstance();
            _ = builder.RegisterType<WindowsManagementClient>().SingleInstance();
            _ = builder.RegisterType<CitrixAdcClient>().SingleInstance();
            _ = builder.RegisterType<RemoteHelperClient>().SingleInstance();
            _ = builder.RegisterType<IISRemoteHelperClient>().SingleInstance();
            _ = builder.RegisterType<IISClient>().As<IIISClient>().InstancePerLifetimeScope();
            _ = builder.RegisterType<IISHelper>().SingleInstance();
            _ = builder.RegisterType<ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AccountManager>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<ZeroSsl>().SingleInstance();
            _ = builder.RegisterType<OrderManager>().SingleInstance();
            _ = builder.RegisterType<PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<CertificateService>().As<ICertificateService>().SingleInstance();
            _ = builder.RegisterType<SecretServiceManager>().SingleInstance();
            _ = builder.RegisterType<JsonSecretService>().As<ISecretService>().SingleInstance();
            _ = builder.RegisterType<TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<RenewalValidator>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.RegisterType<RenewalCreator>().SingleInstance();
            _ = builder.RegisterType<ArgumentsInputService>().SingleInstance();

            _ = builder.RegisterType<Wacs>();
        }
    }
}