using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("abc4f879-7529-481b-95ea-95f98d1ceed7")]
    internal class DelegationOptions : ValidationPluginOptions<Delegation>
    {
        public const string DefaultZone = "fdohdomainvalidation.com";

        public override string Name => "Delegation";
        public override string Description => "Create verification records in DNS delegation zone (https://fdohcerts.com/delegation)";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? Zone { get; set; }
    }
}