using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("5C3DB0FB-840B-469F-B5A7-0635D8E9A93D")]
    internal class CsrOptions : TargetPluginOptions<Csr>
    {
        public static string NameLabel => "CSR";
        public override string Name => NameLabel;
        public override string Description => "CSR created by another program";
        public override string? CommonName {
            get
            {
                if (string.IsNullOrWhiteSpace(CsrFile))
                {
                    return NameLabel;
                }
                var csrString = File.ReadAllText(CsrFile);
                var pem = new PemService().ParsePem<Pkcs10CertificationRequest>(csrString);
                if (pem == null)
                {
                    return NameLabel;
                }
                var info = pem.GetCertificationRequestInfo();
                return Csr.ParseCn(info).Value;
            }
            set => throw new NotImplementedException();
        }
        public override List<string>? AlternativeNames {
            get
            {
                if (string.IsNullOrWhiteSpace(CsrFile))
                {
                    return new List<string>();
                }
                var csrString = File.ReadAllText(CsrFile);
                var pem = new PemService().ParsePem<Pkcs10CertificationRequest>(csrString);
                if (pem == null)
                {
                    return new List<string>();
                }
                var info = pem.GetCertificationRequestInfo();
                return Csr.ParseSan(info).Select(x => x.Value).ToList();
            }
            set => throw new NotImplementedException();
        }
        public string? CsrFile { get; set; }
        public string? PkFile { get; set; }
    }
}
