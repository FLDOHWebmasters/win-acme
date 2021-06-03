using System.Collections.Generic;

namespace PKISharp.WACS.Configuration
{
    public interface IArgumentsParser
    {
        T? GetArguments<T>() where T : class, new();
        bool Validate();
        bool Active();
        IEnumerable<string> SecretArguments { get; }
        void ShowCommandLine();
        void ShowArguments();
    }
}
