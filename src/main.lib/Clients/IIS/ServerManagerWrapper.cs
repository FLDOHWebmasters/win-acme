using Microsoft.Web.Administration;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    public class ServerManagerWrapper : IServerManager
    {
        private readonly ServerManager _serverManager = new();

        public IEnumerable<Site> Sites => _serverManager.Sites.AsEnumerable();

        public SiteDefaults SiteDefaults => _serverManager.SiteDefaults;

        public void CommitChanges() => _serverManager.CommitChanges();

        public void Dispose() => _serverManager.Dispose();
    }
}
