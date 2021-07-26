using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class TomcatOptions : InstallationPluginOptions<Tomcat>
    {
        public string? HostName { get; set; }

        public override string Name => "Tomcat";
        public override string Description => "Update a certificate via the Java keystore and Tomcat server.xml file";
    }
}
