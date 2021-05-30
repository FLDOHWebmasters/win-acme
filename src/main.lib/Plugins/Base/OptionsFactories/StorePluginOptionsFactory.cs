using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class StorePluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        IStorePluginOptionsFactory
        where TPlugin : IStorePlugin
        where TOptions : StorePluginOptions, new()
    {
        public abstract Task<TOptions?> Acquire(IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions?> Default();
        async Task<StorePluginOptions?> IPluginOptionsFactory<StorePluginOptions>.Acquire(IInputService inputService, RunLevel runLevel) => await Acquire(inputService, runLevel);
        async Task<StorePluginOptions?> IPluginOptionsFactory<StorePluginOptions>.Default() => await Default();
    }



}
