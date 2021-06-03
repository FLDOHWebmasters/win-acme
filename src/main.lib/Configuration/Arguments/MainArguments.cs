using PKISharp.WACS.Plugins.TargetPlugins;

namespace PKISharp.WACS.Configuration.Arguments
{
    public class MainArguments : BaseArguments, IMainArguments
    {
        public override string Name => "Main";
        public override bool Active()
        {
            return
                !string.IsNullOrEmpty(FriendlyName) ||
                !string.IsNullOrEmpty(Installation) ||
                !string.IsNullOrEmpty(Store) ||
                !string.IsNullOrEmpty(Order) ||
                !string.IsNullOrEmpty(Csr) ||
                !string.IsNullOrEmpty(Target) ||
                !string.IsNullOrEmpty(Source) ||
                !string.IsNullOrEmpty(Validation);
        }

        public bool HasFilter =>
            !string.IsNullOrEmpty(Id) ||
            !string.IsNullOrEmpty(FriendlyName);

        // Basic options

        [CommandLine(Description = "Address of the ACMEv2 server to use. The default endpoint can be modified in settings.json.")]
        public virtual string BaseUri { get; set; } = "";

        [CommandLine(Description = "Import scheduled renewals from version 1.9.x in unattended mode.")]
        public virtual bool Import { get; set; }

        [CommandLine(Description = "[--import] When importing scheduled renewals from version 1.9.x, this argument can change the address of the ACMEv1 server to import from. The default endpoint to import from can be modified in settings.json.")]
        public virtual string? ImportBaseUri { get; set; }

        [CommandLine(Description = "Enables testing behaviours in the program which may help with troubleshooting. By default this also switches the --baseuri to the ACME test endpoint. The default endpoint for test mode can be modified in settings.json.")]
        public virtual bool Test { get; set; }

        [CommandLine(Description = "Print additional log messages to console for troubleshooting and bug reports.")]
        public virtual bool Verbose { get; set; }

        [CommandLine(Description = "Show information about all available command line options.")]
        public virtual bool Help { get; set; }

        [CommandLine(Description = "Show version information.")]
        public virtual bool Version { get; set; }

        // Renewal

        [CommandLine(Description = "Renew any certificates that are due. This argument is used by the scheduled task. Note that it's not possible to change certificate properties and renew at the same time.")]
        public virtual bool Renew { get; set; }

        [CommandLine(Description = "Force renewal when used together with --renew. Otherwise bypasses the certificate cache on new certificate requests.")]
        public virtual bool Force { get; set; }

        // Commands

        [CommandLine(Description = "Cancel renewal specified by the --friendlyname or --id arguments.")]
        public virtual bool Cancel { get; set; }

        [CommandLine(Description = "Revoke the most recently issued certificate for the renewal specified by the --friendlyname or --id arguments.")]
        public virtual bool Revoke { get; set; }

        [CommandLine(Description = "List all created renewals in unattended mode.")]
        public virtual bool List { get; set; }

        [CommandLine(Description = "Rewrites all renewal information using current EncryptConfig setting")]
        public virtual bool Encrypt { get; set; }

        // Targeting

        [CommandLine(Description = "[--source|--cancel|--renew|--revoke] Id of a new or existing renewal, can be used to override the default when creating a new renewal or to specify a specific renewal for other commands.")]
        public virtual string? Id { get; set; }

        [CommandLine(Description = "[--source|--cancel|--renew|--revoke] Friendly name of a new or existing renewal, can be used to override the default when creating a new renewal or to specify a specific renewal for other commands. In the latter case a pattern might be used. " + IISArguments.PatternExamples)]
        public virtual string? FriendlyName { get; set; }

        [CommandLine(Description = "Specify which target plugin to run, bypassing the main menu and triggering unattended mode.", Obsolete = true)]
        public virtual string? Target { get; set; }

        [CommandLine(Description = "Specify which source plugin to run, bypassing the main menu and triggering unattended mode.")]
        public virtual string? Source { get; set; }

        [CommandLine(Description = "Specify which validation plugin to run. If none is specified, SelfHosting validation will be chosen as the default.")]
        public virtual string? Validation { get; set; }

        [CommandLine(Description = "Specify which validation mode to use. HTTP-01 is the default.", Default = Constants.Http01ChallengeType)]
        public virtual string? ValidationMode { get; set; }

        [CommandLine(Description = "Specify which order plugin to use. Single is the default.")]
        public virtual string? Order { get; set; }

        [CommandLine(Description = "Specify which CSR plugin to use. RSA is the default.")]
        public virtual string? Csr { get; set; }

        [CommandLine(Description = "Specify which store plugin to use. CertificateStore is the default. This may be a comma-separated list.")]
        public virtual string? Store { get; set; }

        [CommandLine(Description = "Specify which installation plugins to use. IIS is the default. This may be a comma-separated list.")]
        public virtual string? Installation { get; set; }

        // Misc

        [CommandLine(Description = "[--test] Close the application when complete, which usually does not happen when test mode is active. Useful to test unattended operation.")]
        public virtual bool CloseOnFinish { get; set; }

        [CommandLine(Description = "Hide sites that have existing https bindings from interactive mode.")]
        public virtual bool HideHttps { get; set; }

        [CommandLine(Description = "Do not create (or offer to update) the scheduled task.")]
        public virtual bool NoTaskScheduler { get; set; }

        [CommandLine(Description = "(Obsolete) Avoid the question about specifying the task scheduler user, as such defaulting to the SYSTEM account.")]
        public virtual bool UseDefaultTaskUser { get; set; }

        [CommandLine(Description = "Create or update the scheduled task according to the current settings.")]
        public virtual bool SetupTaskScheduler { get; set; }
    }
}