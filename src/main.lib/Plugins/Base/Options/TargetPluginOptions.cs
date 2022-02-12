﻿using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class TargetPluginOptions : PluginOptions
    {
        public override string Name => throw new NotImplementedException();
        public override string Description => throw new NotImplementedException();
        public override Type Instance => throw new NotImplementedException();
        public virtual string? CommonName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public abstract class TargetPluginOptions<T> : TargetPluginOptions where T : ITargetPlugin
    {
        public abstract override string Name { get; }
        public abstract override string Description { get; }
        public abstract override string? CommonName { get; set; }

        public override void Show(IInputService input)
        {
            input.Show(null, "[Source]");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
        }

        public override Type Instance => typeof(T);
    }
}
