namespace PKISharp.WACS.Configuration.Arguments
{
    public interface IMainArguments
    {
        bool HasFilter { get; }

        // Basic options
        string BaseUri { get; set; }
        bool Import { get; set; }
        string? ImportBaseUri { get; set; }
        bool Test { get; set; }
        bool Verbose { get; set; }
        bool Help { get; set; }
        bool Version { get; set; }

        // Renewal
        bool Renew { get; set; }
        bool Force { get; set; }

        // Commands
        bool Cancel { get; set; }
        bool Revoke { get; set; }
        bool List { get; set; }
        bool Encrypt { get; set; }

        // Targeting
        string? Id { get; set; }
        string? FriendlyName { get; set; }
        string? Target { get; set; }
        string? Source { get; set; }
        string? Validation { get; set; }
        string? ValidationMode { get; set; }
        string? Order { get; set; }
        string? Csr { get; set; }
        string? Store { get; set; }
        string? Installation { get; set; }

        // Misc
        bool CloseOnFinish { get; set; }
        bool HideHttps { get; set; }
        bool NoTaskScheduler { get; set; }
        bool UseDefaultTaskUser { get; set; }
        bool SetupTaskScheduler { get; set; }
    }
}
