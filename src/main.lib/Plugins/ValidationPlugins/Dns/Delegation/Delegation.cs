using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Delegation : DnsValidation<Delegation>
    {
        private readonly ISystemManagementClient _client;
        private readonly DelegationOptions _options;

        public Delegation(
            LookupClientProvider dnsLookup,
            ISystemManagementClient dnsClient,
            ILogService log,
            ISettingsService settings,
            DelegationOptions options) :
            base(dnsLookup, log, settings)
        {
            _client = dnsClient;
            _options = options;
        }

        /// <summary>
        /// Send API call to the acme-dns server
        /// </summary>
        /// <param name="recordName"></param>
        /// <param name="token"></param>
        public override async Task<bool> CreateRecord(DnsValidationRecord record)
            => await Task.Run(() => _client.CreateTxtRecord(_options.Zone!, record.Context.Identifier, record.Value));

        public override async Task DeleteRecord(DnsValidationRecord record)
            => await Task.Run(() => _client.DeleteTxtRecord(_options.Zone!, record.Context.Identifier, record.Value));
    }
}
