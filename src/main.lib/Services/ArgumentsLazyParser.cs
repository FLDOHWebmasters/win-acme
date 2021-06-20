using System;
using System.Collections.Generic;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Configuration
{
    public interface IArgumentsSetter
    {
        void SetArguments(string[] args);
    }

    public class ArgumentsLazyParser : IArgumentsParser, IArgumentsSetter
    {
        private readonly ILogService _log;
        private readonly IPluginService _plugins;
        private IArgumentsParser? _parser;

        public ArgumentsLazyParser(ILogService log, IPluginService plugins)
        {
            _log = log;
            _plugins = plugins;
        }

        private T? IfInitialized<T>(Func<IArgumentsParser, T> then, bool throwIfNot = true)
        {
            if (_parser == null)
            {
                if (throwIfNot)
                {
                    throw new Exception("Arguments must be set first.");
                }
                return default;
            }
            lock (this)
            {
                return then(_parser);
            }
        }

        public void SetArguments(string[] args)
        {
            lock (this)
            {
                _parser = new ArgumentsParser(_log, _plugins, args);
                if (!_parser.Validate())
                {
                    throw new ArgumentException("Invalid arguments.", nameof(args));
                }
            }
        }

        public T? GetArguments<T>() where T : class, new() => IfInitialized(parser => parser.GetArguments<T>());

        public bool Validate() => IfInitialized(parser => parser.Validate(), false);

        public bool Active() => IfInitialized(parser => parser.Active());

        public IEnumerable<string> SecretArguments => IfInitialized(parser => parser.SecretArguments) ?? Array.Empty<string>();

        public void ShowCommandLine() => _ = IfInitialized(parser => { parser.ShowCommandLine(); return 0; });

        public void ShowArguments() => _ = IfInitialized(parser => { parser.ShowArguments(); return 0; });
    }
}
