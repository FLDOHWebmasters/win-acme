using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Clients.IIS
{
    public interface IServerManager : IDisposable
    {
        IEnumerable<Site> Sites { get; }

        SiteDefaults SiteDefaults { get; }

        void CommitChanges();
    }
}
