using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// TargetPluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class TargetPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        ITargetPluginOptionsFactory
        where TPlugin : ITargetPlugin
        where TOptions : TargetPluginOptions, new()
    {
        public abstract Task<TOptions?> Acquire(IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions?> Default();
        async Task<TargetPluginOptions?> IPluginOptionsFactory<TargetPluginOptions>.Acquire(IInputService inputService, RunLevel runLevel) => await Acquire(inputService, runLevel);
        async Task<TargetPluginOptions?> IPluginOptionsFactory<TargetPluginOptions>.Default() => await Default();
    }
}
