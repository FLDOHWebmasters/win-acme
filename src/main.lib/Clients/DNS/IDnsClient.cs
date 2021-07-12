using System;

namespace PKISharp.WACS.Clients.DNS
{
    public interface IDnsClient
    {
        bool CreateTxtRecord(string zone, string hostName, string descriptiveText);
        void DeleteTxtRecord(string zone, string hostName, string descriptiveText);
    }
}
