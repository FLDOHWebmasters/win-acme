using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("435d82b7-002a-4ff1-8f67-678385935485")]
    public class RemoteIISOptions : TargetPluginOptions<RemoteIIS>
    {
        public override string Name => throw new System.NotImplementedException();

        public override string Description => throw new System.NotImplementedException();

        public override string? CommonName { get; set; }
        public override List<string>? AlternativeNames { get; set; }
    }
}
