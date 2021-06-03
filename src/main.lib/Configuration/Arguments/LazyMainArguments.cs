using System;
using System.Linq;

namespace PKISharp.WACS.Configuration.Arguments
{
    public class LazyMainArguments : MainArguments
    {
        private readonly IArgumentsParser _parser;
        bool validated;

        public LazyMainArguments(IArgumentsParser parser) => _parser = parser;

        private T? LazyInitialize<T>(Func<T> get)
        {
            lock (_parser)
            {
                if (!validated)
                {
                    if (!_parser.Validate())
                    {
                        return default;
                    }
                    var args = _parser.GetArguments<MainArguments>();
                    foreach (var prop in typeof(MainArguments).GetProperties().Where(x => x.CanWrite))
                    {
                        prop.SetValue(this, prop.GetValue(args, null), null);
                    }
                    validated = true;
                }
            }
            return get();
        }

        public override string BaseUri { get => LazyInitialize(() => base.BaseUri)!; set => base.BaseUri = value; }
        public override bool Import { get => LazyInitialize(() => base.Import); set => base.Import = value; }
        public override string? ImportBaseUri { get => LazyInitialize(() => base.ImportBaseUri); set => base.ImportBaseUri = value; }
        public override bool Test { get => LazyInitialize(() => base.Test); set => base.Test = value; }
        public override bool Verbose { get => LazyInitialize(() => base.Verbose); set => base.Verbose = value; }
        public override bool Help { get => LazyInitialize(() => base.Help); set => base.Help = value; }
        public override bool Version { get => LazyInitialize(() => base.Version); set => base.Version = value; }

        // Renewal
        public override bool Renew { get => LazyInitialize(() => base.Renew); set => base.Renew = value; }
        public override bool Force { get => LazyInitialize(() => base.Force); set => base.Force = value; }

        // Commands
        public override bool Cancel { get => LazyInitialize(() => base.Cancel); set => base.Cancel = value; }
        public override bool Revoke { get => LazyInitialize(() => base.Revoke); set => base.Revoke = value; }
        public override bool List { get => LazyInitialize(() => base.List); set => base.List = value; }
        public override bool Encrypt { get => LazyInitialize(() => base.Encrypt); set => base.Encrypt = value; }

        // Targeting
        public override string? Id { get => LazyInitialize(() => base.Id); set => base.Id = value; }
        public override string? FriendlyName { get => LazyInitialize(() => base.FriendlyName); set => base.FriendlyName = value; }
        public override string? Target { get => LazyInitialize(() => base.Target); set => base.Target = value; }
        public override string? Source { get => LazyInitialize(() => base.Source); set => base.Source = value; }
        public override string? Validation { get => LazyInitialize(() => base.Validation); set => base.Validation = value; }
        public override string? ValidationMode { get => LazyInitialize(() => base.ValidationMode); set => base.ValidationMode = value; }
        public override string? Order { get => LazyInitialize(() => base.Order); set => base.Order = value; }
        public override string? Csr { get => LazyInitialize(() => base.Csr); set => base.Csr = value; }
        public override string? Store { get => LazyInitialize(() => base.Store); set => base.Store = value; }
        public override string? Installation { get => LazyInitialize(() => base.Installation); set => base.Installation = value; }

        // Misc
        public override bool CloseOnFinish { get => LazyInitialize(() => base.CloseOnFinish); set => base.CloseOnFinish = value; }
        public override bool HideHttps { get => LazyInitialize(() => base.HideHttps); set => base.HideHttps = value; }
        public override bool NoTaskScheduler { get => LazyInitialize(() => base.NoTaskScheduler); set => base.NoTaskScheduler = value; }
        public override bool UseDefaultTaskUser { get => LazyInitialize(() => base.UseDefaultTaskUser); set => base.UseDefaultTaskUser = value; }
        public override bool SetupTaskScheduler { get => LazyInitialize(() => base.SetupTaskScheduler); set => base.SetupTaskScheduler = value; }
    }
}
