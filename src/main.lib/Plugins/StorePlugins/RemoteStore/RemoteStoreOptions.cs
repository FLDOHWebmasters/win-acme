using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [Plugin("f51c2197-29c8-4102-a341-751ebd352c29")]
    public class RemoteStoreOptions : StorePluginOptions<RemoteStore>
    {
        public override string Name => "Remote Certificate Store";

        public override string Description => "Windows Certificate Store on another machine";

        /// <summary>
        /// Name of the certificate store to use (defaults to WebHosting)
        /// </summary>
        public string? StoreName { get; set; }

        /// <summary>
        /// List of additional principals (besides the owners of the store) that should get full control permissions on the private key of the certificate
        /// </summary>
        public List<string>? AclFullControl { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            if (!string.IsNullOrEmpty(StoreName))
            {
                input.Show("Store", StoreName, level: 1);
            }
            if (AclFullControl != null)
            {
                input.Show("AclFullControl", string.Join(",", AclFullControl), level: 1);
            }
        }
    }
}
