using System;

namespace CsprojClassLibraryMultiFramework
{
    public class MultiFrameworkClass
    {
#if NET
        public string Name { get; } = ".NET v6.0 MultiFrameworkClass";
#else
        public string Name { get; } = ".NETFX v4.8 MultiFrameworkClass";
#endif
    }
}
