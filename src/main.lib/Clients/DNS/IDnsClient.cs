using System;

namespace PKISharp.WACS.Clients.DNS
{
    public interface IDnsClient
    {
        bool AddTxtRecord(string zone, string hostName, string descriptiveText);
    }
}
