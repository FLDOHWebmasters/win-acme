using System;
using System.Linq;

namespace PKISharp.WACS.Configuration.Arguments
{
    public class LazyMainArguments : IMainArguments
    {
        private readonly IArgumentsParser _parser;
        private MainArguments? _args;

        public LazyMainArguments(IArgumentsParser parser) => _parser = parser;

        private void IfInitialized(Action<MainArguments> set)
        {
            if (_args != null)
            {
                set(_args);
            }
        }

        private T? LazyInitialize<T>(Func<MainArguments, T> get)
        {
            lock (_parser)
            {
                if (_args == null)
                {
                    if (!_parser.Validate())
                    {
                        return default;
                    }
                    _args = _parser.GetArguments<MainArguments>()!;
                }
            }
            return get(_args!);
        }

        public bool HasFilter => LazyInitialize(args => args.HasFilter);

        // Basic options
        public string BaseUri { get => LazyInitialize(args => args.BaseUri)!; set => IfInitialized(args => args.BaseUri = value); }
        public bool Import { get => LazyInitialize(args => args.Import); set => IfInitialized(args => args.Import = value); }
        public string? ImportBaseUri { get => LazyInitialize(args => args.ImportBaseUri); set => IfInitialized(args => args.ImportBaseUri = value); }
        public bool Test { get => LazyInitialize(args => args.Test); set => IfInitialized(args => args.Test = value); }
        public bool Verbose { get => LazyInitialize(args => args.Verbose); set => IfInitialized(args => args.Verbose = value); }
        public bool Help { get => LazyInitialize(args => args.Help); set => IfInitialized(args => args.Help = value); }
        public bool Version { get => LazyInitialize(args => args.Version); set => IfInitialized(args => args.Version = value); }

        // Renewal
        public bool Renew { get => LazyInitialize(args => args.Renew); set => IfInitialized(args => args.Renew = value); }
        public bool Force { get => LazyInitialize(args => args.Force); set => IfInitialized(args => args.Force = value); }

        // Commands
        public bool Cancel { get => LazyInitialize(args => args.Cancel); set => IfInitialized(args => args.Cancel = value); }
        public bool Revoke { get => LazyInitialize(args => args.Revoke); set => IfInitialized(args => args.Revoke = value); }
        public bool List { get => LazyInitialize(args => args.List); set => IfInitialized(args => args.List = value); }
        public bool Encrypt { get => LazyInitialize(args => args.Encrypt); set => IfInitialized(args => args.Encrypt = value); }

        // Targeting
        public string? Id { get => LazyInitialize(args => args.Id); set => IfInitialized(args => args.Id = value); }
        public string? FriendlyName { get => LazyInitialize(args => args.FriendlyName); set => IfInitialized(args => args.FriendlyName = value); }
        public string? Target { get => LazyInitialize(args => args.Target); set => IfInitialized(args => args.Target = value); }
        public string? Source { get => LazyInitialize(args => args.Source); set => IfInitialized(args => args.Source = value); }
        public string? Validation { get => LazyInitialize(args => args.Validation); set => IfInitialized(args => args.Validation = value); }
        public string? ValidationMode { get => LazyInitialize(args => args.ValidationMode); set => IfInitialized(args => args.ValidationMode = value); }
        public string? Order { get => LazyInitialize(args => args.Order); set => IfInitialized(args => args.Order = value); }
        public string? Csr { get => LazyInitialize(args => args.Csr); set => IfInitialized(args => args.Csr = value); }
        public string? Store { get => LazyInitialize(args => args.Store); set => IfInitialized(args => args.Store = value); }
        public string? Installation { get => LazyInitialize(args => args.Installation); set => IfInitialized(args => args.Installation = value); }

        // Misc
        public bool CloseOnFinish { get => LazyInitialize(args => args.CloseOnFinish); set => IfInitialized(args => args.CloseOnFinish = value); }
        public bool HideHttps { get => LazyInitialize(args => args.HideHttps); set => IfInitialized(args => args.HideHttps = value); }
        public bool NoTaskScheduler { get => LazyInitialize(args => args.NoTaskScheduler); set => IfInitialized(args => args.NoTaskScheduler = value); }
        public bool UseDefaultTaskUser { get => LazyInitialize(args => args.UseDefaultTaskUser); set => IfInitialized(args => args.UseDefaultTaskUser = value); }
        public bool SetupTaskScheduler { get => LazyInitialize(args => args.SetupTaskScheduler); set => IfInitialized(args => args.SetupTaskScheduler = value); }
    }
}
