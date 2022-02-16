using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    public class RemoteIIS : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IISRemoteHelperClient _client;

        public RemoteIIS(ILogService logService, IISRemoteHelperClient client)
        {
            _log = logService;
            _client = client;
        }

        public (bool, string?) Disabled => (false, null);

        public Task<Target> Generate()
        {
            _log.Verbose($"{nameof(RemoteIIS)}.Generate()");
            return _client.Generate()!;
        }
    }
}
