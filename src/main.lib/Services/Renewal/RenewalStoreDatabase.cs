using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.InstallationPlugins;

namespace PKISharp.WACS.Services
{
    public class RenewalStoreDatabase : RenewalStoreSecondary
    {
        private readonly string _mongoDbConnectionString;
        private readonly ICertificateService _certificateService;
        private readonly ILogService _log;

        public RenewalStoreDatabase(ISettingsService settings, ICertificateService certificateService, ILogService log)
        {
            _mongoDbConnectionString = settings.Security?.MongoDbConnectionString ?? string.Empty;
            _certificateService = certificateService;
            _log = log;
        }

        public override void Cancel(Renewal renewal)
        {
            var settings = MongoClientSettings.FromConnectionString(_mongoDbConnectionString);
            var client = new MongoClient(settings);
            var database = client.GetDatabase("FDOH");
            var collection = database.GetCollection<BsonDocument>("Cert Database");
            var filter = Builders<BsonDocument>.Filter.Eq("Certificate_Name", renewal.FriendlyName);
            var update = Builders<BsonDocument>.Update.Set("Renewal_Canceled", true);
            var result = collection.UpdateOne(filter, update);
            if (result.MatchedCount > 1)
            {
                _log.Warning($"RenewalStoreDatabase.Cancel(): More than certificate matched for {renewal.FriendlyName}");
            }
            if (result.ModifiedCount != 1)
            {
                _log.Warning($"RenewalStoreDatabase.Cancel(): Certificate was not modified for {renewal.FriendlyName}");
            }
        }

        public override void Clear()
        {
            var settings = MongoClientSettings.FromConnectionString(_mongoDbConnectionString);
            var client = new MongoClient(settings);
            var database = client.GetDatabase("FDOH");
            var collection = database.GetCollection<BsonDocument>("Cert Database");
            var filter = Builders<BsonDocument>.Filter.Empty;
            var update = Builders<BsonDocument>.Update.Set("Renewal_Canceled", true);
            var result = collection.UpdateMany(filter, update);
            if (result.MatchedCount == 0)
            {
                _log.Warning($"RenewalStoreDatabase.Clear(): No certificates found");
            }
            if (result.ModifiedCount != result.MatchedCount)
            {
                _log.Warning($"RenewalStoreDatabase.Clear(): {result.ModifiedCount} modified of {result.MatchedCount} matched");
            }
        }

        public override void Import(Renewal renewal) => Write(renewal);

        public override void Save(Renewal renewal, RenewResult result) => Write(renewal, result);

        private void Write(Renewal renewal, RenewResult? result = null)
        {
            var friendlyName = renewal.LastFriendlyName
                ?? throw new ArgumentNullException(nameof(renewal.LastFriendlyName));
            _log.Debug($"RenewalStoreDatabase.Write() renewal.FriendlyName {friendlyName}");
            var store = renewal.StorePluginOptions.First().Name;
            var isCentralSSL = string.Equals(store, "centralssl", StringComparison.OrdinalIgnoreCase);
            var isPemFiles = string.Equals(store, "pemfiles", StringComparison.OrdinalIgnoreCase);
            var challengeType = renewal.ValidationPluginOptions.Name;
            var installPlugin = renewal.InstallationPluginOptions.FirstOrDefault();
            var installationType = installPlugin?.Name ?? "None";
            var subjectAlternateNames = NotificationService.NotificationHosts(renewal, _certificateService, true);
            var commonName = subjectAlternateNames.FirstOrDefault() ?? friendlyName;
            var certificateType = subjectAlternateNames.Length > 1 ? "Multiple" : "Single";
            var host = "Unknown";
            var siteName = "Unknown";
            if (installPlugin is CitrixAdcOptions options && options.NitroHost is not null)
            {
                host = options.NitroHost;
                siteName = commonName;
            }
            else if (installPlugin is IISWebOptions woptions && woptions.Host is not null)
            {
                host = woptions.Host;
                if (woptions.SiteId > 0)
                {
                    var site = IISClient.GetWebSite(host, woptions.SiteId ?? 0, _log);
                    if (site != null)
                    {
                        siteName = site.Name;
                    }
                }
            }

            var settings = MongoClientSettings.FromConnectionString(_mongoDbConnectionString);
            var client = new MongoClient(settings);
            var database = client.GetDatabase("WebTeam");
            var collection = database.GetCollection<BsonDocument>("Cert Database");
            var filter = Builders<BsonDocument>.Filter.Eq("Certificate_Name", commonName);
            var document = collection.Find(filter).FirstOrDefault();
            var isNew = document == null;
            if (isNew)
            {
                document = new BsonDocument {
                    { "Additional_Notes", "" },
                    { "Associated_Subdomains", "" },
                    { "Certificate_Name", commonName },
                    { "Contact_1", new BsonArray { "webmasters@flhealth.gov" } },
                    { "History", "" },
                    { "Installation_Point", new BsonArray { installationType } },
                    { "Installation_Team", "" },
                    { "Notes", "Certificate Manager" },
                    { "Single_Multiple", "" },
                    { "status", "" },
                    { "vendor", "" },
                };
            }
            var dueDate = renewal.GetDueDate() ?? DateTime.Today;
            document!.Set("Certificate_Authority", "Let's Encrypt");
            document.Set("Certificate_Format", isCentralSSL ? "Windows PKCS 12" : isPemFiles ? "Linux PEM file" : store);
            document.Set("Certificate_Type", certificateType);
            document.Set("Challenge_Type", challengeType);
            document.Set("Installation_Type", installationType);
            document.Set("Renewal_Canceled", false);
            document.Set("SANs", string.Join(", ", subjectAlternateNames));
            document.Set("Server", host);
            document.Set("Site_Name", siteName);
            document.Set("Valid_Through", dueDate);
            document.Set("Wildcard_Name", commonName.StartsWith("*.") ? commonName : string.Empty);
            document.Set("expiration_date", dueDate.AddDays(60));
            document.Set("purchase_date", dueDate.AddDays(-30));
            if (result != null)
            {
                var renewals = document.GetValue("Renewals", new BsonArray()).AsBsonArray;
                renewals.Add(new BsonDocument {
                    { "RenewDate", result.Date },
                    { "IsAborted", result.Abort },
                    { "IsSuccess", result.Success },
                    { "ErrorMessages", new BsonArray(result.ErrorMessages) },
                });
                document.Set("Renewals", renewals);
            }
            if (isNew)
            {
                collection.InsertOne(document);
            }
            else
            {
                collection.ReplaceOne(filter, document);
            }
        }
    }
}
