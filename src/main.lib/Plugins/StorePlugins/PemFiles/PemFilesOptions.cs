﻿using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [Plugin("e57c70e4-cd60-4ba6-80f6-a41703e21031")]
    public class PemFilesOptions : StorePluginOptions<PemFiles>
    {
        internal const string PluginName = "PemFiles";
        public override string Name => PluginName;
        public override string Description => "PEM encoded files (Apache, nginx, etc.)";

        /// <summary>
        /// PemFiles password
        /// </summary>
        public ProtectedString? PemPassword { get; set; }

        /// <summary>
        /// Path to the .pem directory
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", Path, level: 1);
            input.Show("Password", string.IsNullOrEmpty(PemPassword?.Value) ? "[Default from settings.json]" : PemPassword.DisplayValue, level: 2);
        }
    }
}
