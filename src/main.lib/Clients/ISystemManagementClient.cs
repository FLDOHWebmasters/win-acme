using System;

namespace PKISharp.WACS.Clients
{
    public interface ISystemManagementClient
    {
        bool CreateTxtRecord(string zone, string hostName, string descriptiveText);
        void DeleteTxtRecord(string zone, string hostName, string descriptiveText);
        //void ExecuteCommandLine(string hostName, string commandLine);
    }
}
