using System;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    public class CitrixADCArguments : BaseArguments
    {
        public const string NitroIpAddressParameterName = "nitroipaddress";
        public const string NitroUsernameParameterName = "nitrousername";
        public const string NitroPasswordParameterName = "nitropassword";

        public override string Name => "Citrix ADC plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation adc";

        [CommandLine(Name = NitroIpAddressParameterName, Description = "Host name or IP address of Citrix ADC Nitro API. Defaults to " + CitrixAdcClient.DefaultNitroHost + ".")]
        public string? NitroIpAddress { get; set; }

        [CommandLine(Name = NitroUsernameParameterName, Description = "Username for Citrix ADC Nitro API. Defaults to " + CitrixAdcClient.DefaultNitroUsername + ".")]
        public string? NitroUsername { get; set; }

        [CommandLine(Name = NitroPasswordParameterName, Description = "Password for Citrix ADC Nitro API. Defaults to (encrypted) " + CitrixAdcClient.DefaultNitroPasswordProtected + ".")]
        public string? NitroPassword { get; set; }
    }
}
