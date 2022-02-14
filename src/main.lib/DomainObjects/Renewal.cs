using CertificateManager.Core.Extensions;
using Newtonsoft.Json;
using PKISharp.WACS.Configuration.Settings;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Main unit of work for the program, contains all the information 
    /// required to generate a target, do the validation, store the resulting
    /// certificate somewhere and finally run installation steps to update 
    /// software.
    /// </summary>
    [DebuggerDisplay("Renewal {Id}: {FriendlyName}")]
    public class Renewal
    {
        internal static Renewal Create(string? id, ScheduledTaskSettings settings, PasswordGenerator generator)
        {
            var ret = new Renewal
            {
                New = true,
                Id = string.IsNullOrEmpty(id) ? ShortGuid.NewGuid().ToString() : id,
                PfxPassword = new ProtectedString(generator.Generate()),
                Settings = settings
            };
            return ret;
        }

        /// <summary>
        /// Is this renewal a test?
        /// </summary>
        [JsonIgnore]
        internal bool Test { get; set; }

        /// <summary>
        /// Has this renewal been changed?
        /// </summary>
        [JsonIgnore]
        public bool Updated { get; set; }

        /// <summary>
        /// Has this renewal been deleted?
        /// </summary>
        [JsonIgnore]
        public bool Deleted { get; set; }

        /// <summary>
        /// Current renewal days setting, stored 
        /// here as a shortcut because its not 
        /// otherwise available to the instance
        /// </summary>
        [JsonIgnore]
        internal ScheduledTaskSettings Settings { get; set; }

        /// <summary>
        /// Is this renewal new
        /// </summary>
        [JsonIgnore]
        internal bool New { get; set; }

        /// <summary>
        /// Unique identifer for the renewal
        /// </summary>
        public string Id { get; set; } = "";

        [JsonIgnore]
        public List<string> HostNames => InstallationPluginOptions.Select(x => x.HostName!).Where(x => x.NotBlank()).ToList();

        [JsonIgnore]
        [Display(Name = "Common Name")]
        public string? CommonName => TargetPluginOptions.CommonName;

        [JsonIgnore]
        [Display(Name = "Alternate Names")]
        public string? AlternateNamesDisplay => string.Join(" ", (TargetPluginOptions as ManualOptions)?.AlternativeNames.Where(x => x != CommonName) ?? new List<string>());

        /// <summary>
        /// Friendly name for the certificate. If left
        /// blank or empty, the CommonName will be used.
        /// </summary>
        public string? FriendlyName { get; set; }

        [JsonIgnore]
        [Display(Name = "Friendly Name")]
        public string FriendlyNameDisplay => FriendlyName ?? LastFriendlyName ?? CommonName ?? string.Empty;

        /// <summary>
        /// Display name, as the program shows this certificate
        /// in the interface. This is set to the most recently 
        /// used FriendlyName
        /// </summary>
        public string? LastFriendlyName { get; set; }

        /// <summary>
        /// Plain text readable version of the PfxFile password
        /// </summary>
        [JsonProperty(PropertyName = "PfxPasswordProtected")]
        public ProtectedString? PfxPassword { get; set; }

        [JsonIgnore]
        [Display(Name = "Due Date")]
        public DateTime DueDate => GetDueDate() ?? DateTime.Today;
        public DateTime? GetDueDate()
        {
            var lastSuccess = History.LastOrDefault(x => x.Success);
            if (lastSuccess == null) { return null; }
            var firstOccurance = History.First(x => x.ThumbprintSummary == lastSuccess.ThumbprintSummary);
            var defaultDueDate = firstOccurance.Date.AddDays(Settings.RenewalDays).ToLocalTime();
            if (lastSuccess.ExpireDate == null) { return defaultDueDate; }
            var minDays = Settings.RenewalMinimumValidDays ?? 7;
            var expireBasedDueDate = lastSuccess.ExpireDate.Value.AddDays(minDays * -1).ToLocalTime();
            return expireBasedDueDate < defaultDueDate ? expireBasedDueDate : defaultDueDate;
        }

        [JsonIgnore]
        [Display(Name = "Is Due")]
        public bool Due => IsDue();
        public bool IsDue() => GetDueDate() == null || GetDueDate() < DateTime.Now;

        /// <summary>
        /// Store information about TargetPlugin
        /// </summary>
        public TargetPluginOptions TargetPluginOptions { get; set; } = new TargetPluginOptions();

        /// <summary>
        /// Store information about ValidationPlugin
        /// </summary>
        public ValidationPluginOptions ValidationPluginOptions { get; set; } = new ValidationPluginOptions();

        /// <summary>
        /// Store information about CsrPlugin
        /// </summary>
        public CsrPluginOptions? CsrPluginOptions { get; set; }

        /// <summary>
        /// Store information about OrderPlugin
        /// </summary>
        public OrderPluginOptions? OrderPluginOptions { get; set; }

        /// <summary>
        /// Store information about StorePlugin
        /// </summary>
        public List<StorePluginOptions> StorePluginOptions { get; set; } = new List<StorePluginOptions>();

        [JsonIgnore]
        [Display(Name = "Storage")]
        public string StorePluginNames => string.Join(", ", StorePluginOptions.Select(x => x.Name));
        [JsonIgnore]
        [Display(Name = "Installation")]
        public string InstallPluginDetails => string.Join(", ", InstallationPluginOptions.Select(x => x.Details));

        /// <summary>
        /// Store information about InstallationPlugins
        /// </summary>
        public List<InstallationPluginOptions> InstallationPluginOptions { get; set; } = new List<InstallationPluginOptions>();

        /// <summary>
        /// History for this renewal
        /// </summary>
        public List<RenewResult> History { get; set; } = new List<RenewResult>();

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString(null);

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public string ToString(IInputService? inputService)
        {
            var success = History.FindAll(x => x.Success).Count;
            var errors = History.AsEnumerable().Reverse().TakeWhile(x => !x.Success);
            var ret = $"{LastFriendlyName} - renewed {success} time{(success != 1 ? "s" : "")}";
            var due = IsDue();
            var dueDate = GetDueDate();
            if (inputService == null)
            {
                ret += due ? ", due now" : dueDate == null ? "" : $", due after {dueDate}";
            }
            else
            {
                ret += due ? ", due now" : dueDate == null ? "" : $", due after {inputService.FormatDate(dueDate.Value)}";
            }

            if (errors.Any())
            {
                var messages = errors.SelectMany(x => x.ErrorMessages).Where(x => !string.IsNullOrEmpty(x));
                ret += $", {errors.Count()} error{(errors.Count() != 1 ? "s" : "")} like \"{messages.FirstOrDefault() ?? "[null]"}\"";
            }
            return ret;
        }
    }
}
