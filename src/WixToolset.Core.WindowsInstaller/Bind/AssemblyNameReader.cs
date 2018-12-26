// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.IO;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using WixToolset.Data;

    internal static class AssemblyNameReader
    {
        public static AssemblyName ReadAssembly(SourceLineNumber sourceLineNumbers, string assemblyPath, string fileVersion)
        {
            try
            {
                using (var stream = File.OpenRead(assemblyPath))
                using (var peReader = new PEReader(stream))
                {
                    var reader = peReader.GetMetadataReader();
                    var headers = peReader.PEHeaders;

                    var assembly = reader.GetAssemblyDefinition();
                    var attributes = assembly.GetCustomAttributes();

                    var name = ReadString(reader, assembly.Name);
                    var culture = ReadString(reader, assembly.Culture);
                    var architecture = headers.PEHeader.Magic == PEMagic.PE32Plus ? "x64" : (headers.CorHeader.Flags & CorFlags.Requires32Bit) == CorFlags.Requires32Bit ? "x86" : null;
                    var version = assembly.Version.ToString();
                    var publicKeyToken = ReadPublicKeyToken(reader, assembly.PublicKey);

                    // There is a bug in v1 fusion that requires the assembly's "version" attribute
                    // to be equal to or longer than the "fileVersion" in length when its present;
                    // the workaround is to prepend zeroes to the last version number in the assembly
                    // version.
                    var targetNetfx1 = (headers.CorHeader.MajorRuntimeVersion == 2) && (headers.CorHeader.MinorRuntimeVersion == 0);
                    if (targetNetfx1 && !String.IsNullOrEmpty(fileVersion) && fileVersion.Length > version.Length)
                    {
                        var versionParts = version.Split('.');

                        if (versionParts.Length > 0)
                        {
                            var padding = new string('0', fileVersion.Length - version.Length);

                            versionParts[versionParts.Length - 1] = String.Concat(padding, versionParts[versionParts.Length - 1]);
                            version = String.Join(".", versionParts);
                        }
                    }

                    return new AssemblyName(name, culture, version, fileVersion, architecture, publicKeyToken, null);
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is BadImageFormatException || e is InvalidOperationException)
            {
                throw new WixException(ErrorMessages.InvalidAssemblyFile(sourceLineNumbers, assemblyPath, $"{e.GetType().Name}: {e.Message}"));
            }
        }

        public static AssemblyName ReadAssemblyManifest(SourceLineNumber sourceLineNumbers, string manifestPath)
        {
            string win32Type = null;
            string win32Name = null;
            string win32Version = null;
            string win32ProcessorArchitecture = null;
            string win32PublicKeyToken = null;

            // Loading the dom is expensive we want more performant APIs than the DOM
            // Navigator is cheaper than dom.  Perhaps there is a cheaper API still.
            try
            {
                var doc = new XPathDocument(manifestPath);
                var nav = doc.CreateNavigator();
                nav.MoveToRoot();

                // This assumes a particular schema for a win32 manifest and does not
                // provide error checking if the file does not conform to schema.
                // The fallback case here is that nothing is added to the MsiAssemblyName
                // table for an out of tolerance Win32 manifest.  Perhaps warnings needed.
                if (nav.MoveToFirstChild())
                {
                    while (nav.NodeType != XPathNodeType.Element || nav.Name != "assembly")
                    {
                        nav.MoveToNext();
                    }

                    if (nav.MoveToFirstChild())
                    {
                        var hasNextSibling = true;
                        while (nav.NodeType != XPathNodeType.Element || nav.Name != "assemblyIdentity" && hasNextSibling)
                        {
                            hasNextSibling = nav.MoveToNext();
                        }

                        if (!hasNextSibling)
                        {
                            throw new WixException(ErrorMessages.InvalidManifestContent(sourceLineNumbers, manifestPath));
                        }

                        if (nav.MoveToAttribute("type", String.Empty))
                        {
                            win32Type = nav.Value;
                            nav.MoveToParent();
                        }

                        if (nav.MoveToAttribute("name", String.Empty))
                        {
                            win32Name = nav.Value;
                            nav.MoveToParent();
                        }

                        if (nav.MoveToAttribute("version", String.Empty))
                        {
                            win32Version = nav.Value;
                            nav.MoveToParent();
                        }

                        if (nav.MoveToAttribute("processorArchitecture", String.Empty))
                        {
                            win32ProcessorArchitecture = nav.Value;
                            nav.MoveToParent();
                        }

                        if (nav.MoveToAttribute("publicKeyToken", String.Empty))
                        {
                            win32PublicKeyToken = nav.Value;
                            nav.MoveToParent();
                        }
                    }
                }
            }
            catch (FileNotFoundException fe)
            {
                throw new WixException(ErrorMessages.FileNotFound(sourceLineNumbers, fe.FileName, "AssemblyManifest"));
            }
            catch (XmlException xe)
            {
                throw new WixException(ErrorMessages.InvalidXml(sourceLineNumbers, "manifest", xe.Message));
            }

            return new AssemblyName(win32Name, null, win32Version, null, win32ProcessorArchitecture, win32PublicKeyToken, win32Type);
        }

        private static string ReadString(MetadataReader reader, StringHandle handle)
        {
            return handle.IsNil ? null : reader.GetString(handle);
        }

        private static string ReadPublicKeyToken(MetadataReader reader, BlobHandle handle)
        {
            if (handle.IsNil)
            {
                return null;
            }

            var bytes = reader.GetBlobBytes(handle);
            if (bytes.Length == 0)
            {
                return null;
            }

            var result = new StringBuilder();

            // If we have the full public key, calculate the public key token from the
            // last 8 bytes (in reverse order) of the public key's SHA1 hash.
            if (bytes.Length > 8)
            {
                using (var sha1 = SHA1.Create())
                {
                    var hash = sha1.ComputeHash(bytes);

                    for (var i = 1; i <= 8; ++i)
                    {
                        result.Append(hash[hash.Length - i].ToString("X2"));
                    }
                }
            }
            else
            {
                foreach (var b in bytes)
                {
                    result.Append(b.ToString("X2"));
                }
            }

            return result.ToString();
        }
    }
}
