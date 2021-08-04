using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("fd417f8b-a78e-47f5-85ea-b25af0e94a44")]
    public class TomcatOptions : InstallationPluginOptions<Tomcat>
    {
        public string? HostName { get; set; }

        public override string Name => "Tomcat";
        public override string Description => "Update a certificate via the Java keystore and Tomcat server.xml file";
    }
}
