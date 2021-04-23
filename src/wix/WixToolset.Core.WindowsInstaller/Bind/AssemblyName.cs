// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Text;

    internal class AssemblyName
    {
        public AssemblyName(string name, string culture, string version, string fileVersion, string architecture, string publicKeyToken, string type)
        {
            this.Name = name;
            this.Culture = culture ?? "neutral";
            this.Version = version;
            this.FileVersion = fileVersion;
            this.Architecture = architecture;

            this.StrongNamedSigned = !String.IsNullOrEmpty(publicKeyToken);
            this.PublicKeyToken = publicKeyToken;
            this.Type = type;
        }

        public string Name { get; }

        public string Culture { get; }

        public string Version { get; }

        public string FileVersion { get; }

        public string Architecture { get; }

        public string PublicKeyToken { get; }

        public bool StrongNamedSigned { get; }

        public string Type { get; }

        public string GetFullName()
        {
            var assemblyName = new StringBuilder();

            assemblyName.Append(this.Name);
            assemblyName.Append(", Version=");
            assemblyName.Append(this.Version);
            assemblyName.Append(", Culture=");
            assemblyName.Append(this.Culture);
            assemblyName.Append(", PublicKeyToken=");
            assemblyName.Append(this.PublicKeyToken ?? "null");

            if (!String.IsNullOrEmpty(this.Architecture))
            {
                assemblyName.Append(", ProcessorArchitecture=");
                assemblyName.Append(this.Architecture);
            }

            return assemblyName.ToString();
        }
    }
}
