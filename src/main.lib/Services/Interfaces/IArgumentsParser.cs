using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Configuration
{
    public interface IArgumentsParser
    {
        event Action? OnInvalidated;
        T? GetArguments<T>() where T : class, new();
        bool Validate();
        void Invalidate();
        bool Active();
        IEnumerable<string> SecretArguments { get; }
        void ShowCommandLine();
        void ShowArguments();
    }
}
