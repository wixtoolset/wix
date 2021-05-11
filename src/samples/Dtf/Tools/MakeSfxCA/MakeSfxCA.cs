// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dtf.Tools.MakeSfxCA
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Security;
    using System.Text;
    using System.Reflection;
    using Compression;
    using Compression.Cab;
    using Resources;
    using ResourceCollection = Resources.ResourceCollection;

    /// <summary>
    /// Command-line tool for building self-extracting custom action packages.
    /// Appends cabbed CA binaries to SfxCA.dll and fixes up the result's
    /// entry-points and file version to look like the CA module.
    /// </summary>
    public static class MakeSfxCA
    {
        private const string REQUIRED_WI_ASSEMBLY = "WixToolset.Dtf.WindowsInstaller.dll";

        private static TextWriter log;

        /// <summary>
        /// Prints usage text for the tool.
        /// </summary>
        /// <param name="w">Console text writer.</param>
        public static void Usage(TextWriter w)
        {
            w.WriteLine("Deployment Tools Foundation custom action packager version {0}",
                Assembly.GetExecutingAssembly().GetName().Version);
            w.WriteLine("Copyright (C) .NET Foundation and contributors. All rights reserved.");
            w.WriteLine();
            w.WriteLine("Usage: MakeSfxCA <outputca.dll> SfxCA.dll <inputca.dll> [support files ...]");
            w.WriteLine();
            w.WriteLine("Makes a self-extracting managed MSI CA or UI DLL package.");
            w.WriteLine("Support files must include " + MakeSfxCA.REQUIRED_WI_ASSEMBLY);
            w.WriteLine("Support files optionally include CustomAction.config/EmbeddedUI.config");
        }

        /// <summary>
        /// Runs the MakeSfxCA command-line tool.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 on success, nonzero on failure.</returns>
        public static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Usage(Console.Out);
                return 1;
            }

            var output = args[0];
            var sfxDll = args[1];
            var inputs = new string[args.Length - 2];
            Array.Copy(args, 2, inputs, 0, inputs.Length);

            try
            {
                Build(output, sfxDll, inputs, Console.Out);
                return 0;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("Error: Invalid argument: " + ex.Message);
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine("Error: Cannot find file: " + ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: Unexpected error: " + ex);
                return 1;
            }
        }

        /// <summary>
        /// Packages up all the inputs to the output location.
        /// </summary>
        /// <exception cref="Exception">Various exceptions are thrown
        /// if things go wrong.</exception>
        public static void Build(string output, string sfxDll, IList<string> inputs, TextWriter log)
        {
            MakeSfxCA.log = log;

            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentNullException("output");
            }

            if (string.IsNullOrEmpty(sfxDll))
            {
                throw new ArgumentNullException("sfxDll");
            }

            if (inputs == null || inputs.Count == 0)
            {
                throw new ArgumentNullException("inputs");
            }

            if (!File.Exists(sfxDll))
            {
                throw new FileNotFoundException(sfxDll);
            }

            var customActionAssembly = inputs[0];
            if (!File.Exists(customActionAssembly))
            {
                throw new FileNotFoundException(customActionAssembly);
            }

            inputs = MakeSfxCA.SplitList(inputs);

            var inputsMap = MakeSfxCA.GetPackFileMap(inputs);

            var foundWIAssembly = false;
            foreach (var input in inputsMap.Keys)
            {
                if (string.Compare(input, MakeSfxCA.REQUIRED_WI_ASSEMBLY,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    foundWIAssembly = true;
                }
            }

            if (!foundWIAssembly)
            {
                throw new ArgumentException(MakeSfxCA.REQUIRED_WI_ASSEMBLY +
                    " must be included in the list of support files. " +
                    "If using the MSBuild targets, make sure the assembly reference " +
                    "has the Private (Copy Local) flag set.");
            }

            MakeSfxCA.ResolveDependentAssemblies(inputsMap, Path.GetDirectoryName(customActionAssembly));

            var entryPoints = MakeSfxCA.FindEntryPoints(customActionAssembly);
            var uiClass = MakeSfxCA.FindEmbeddedUIClass(customActionAssembly);

            if (entryPoints.Count == 0 && uiClass == null)
            {
                throw new ArgumentException(
                    "No CA or UI entry points found in module: " + customActionAssembly);
            }
            else if (entryPoints.Count > 0 && uiClass != null)
            {
                throw new NotSupportedException(
                    "CA and UI entry points cannot be in the same assembly: " + customActionAssembly);
            }

            var dir = Path.GetDirectoryName(output);
            if (dir.Length > 0 && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (Stream outputStream = File.Create(output))
            {
                MakeSfxCA.WriteEntryModule(sfxDll, outputStream, entryPoints, uiClass);
            }

            MakeSfxCA.CopyVersionResource(customActionAssembly, output);

            MakeSfxCA.PackInputFiles(output, inputsMap);

            log.WriteLine("MakeSfxCA finished: " + new FileInfo(output).FullName);
        }

        /// <summary>
        /// Splits any list items delimited by semicolons into separate items.
        /// </summary>
        /// <param name="list">Read-only input list.</param>
        /// <returns>New list with resulting split items.</returns>
        private static IList<string> SplitList(IList<string> list)
        {
            var newList = new List<string>(list.Count);

            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    foreach (var splitItem in item.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        newList.Add(splitItem);
                    }
                }
            }

            return newList;
        }

        /// <summary>
        /// Sets up a reflection-only assembly-resolve-handler to handle loading dependent assemblies during reflection.
        /// </summary>
        /// <param name="inputFiles">List of input files which include non-GAC dependent assemblies.</param>
        /// <param name="inputDir">Directory to auto-locate additional dependent assemblies.</param>
        /// <remarks>
        /// Also searches the assembly's directory for unspecified dependent assemblies, and adds them
        /// to the list of input files if found.
        /// </remarks>
        private static void ResolveDependentAssemblies(IDictionary<string, string> inputFiles, string inputDir)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += delegate(object sender, ResolveEventArgs args)
            {
                AssemblyName resolveName = new AssemblyName(args.Name);
                Assembly assembly = null;

                // First, try to find the assembly in the list of input files.
                foreach (var inputFile in inputFiles.Values)
                {
                    var inputName = Path.GetFileNameWithoutExtension(inputFile);
                    var inputExtension = Path.GetExtension(inputFile);
                    if (string.Equals(inputName, resolveName.Name, StringComparison.OrdinalIgnoreCase) &&
                        (string.Equals(inputExtension, ".dll", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(inputExtension, ".exe", StringComparison.OrdinalIgnoreCase)))
                    {
                        assembly = MakeSfxCA.TryLoadDependentAssembly(inputFile);

                        if (assembly != null)
                        {
                            break;
                        }
                    }
                }

                // Second, try to find the assembly in the input directory.
                if (assembly == null && inputDir != null)
                {
                    string assemblyPath = null;
                    if (File.Exists(Path.Combine(inputDir, resolveName.Name) + ".dll"))
                    {
                        assemblyPath = Path.Combine(inputDir, resolveName.Name) + ".dll";
                    }
                    else if (File.Exists(Path.Combine(inputDir, resolveName.Name) + ".exe"))
                    {
                        assemblyPath = Path.Combine(inputDir, resolveName.Name) + ".exe";
                    }

                    if (assemblyPath != null)
                    {
                        assembly = MakeSfxCA.TryLoadDependentAssembly(assemblyPath);

                        if (assembly != null)
                        {
                            // Add this detected dependency to the list of files to be packed.
                            inputFiles.Add(Path.GetFileName(assemblyPath), assemblyPath);
                        }
                    }
                }

                // Third, try to load the assembly from the GAC.
                if (assembly == null)
                {
                    try
                    {
                        assembly = Assembly.ReflectionOnlyLoad(args.Name);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }

                if (assembly != null)
                {
                    if (string.Equals(assembly.GetName().ToString(), resolveName.ToString()))
                    {
                        log.WriteLine("    Loaded dependent assembly: " + assembly.Location);
                        return assembly;
                    }

                    log.WriteLine("    Warning: Loaded mismatched dependent assembly: " + assembly.Location);
                    log.WriteLine("      Loaded assembly   : " + assembly.GetName());
                    log.WriteLine("      Reference assembly: " + resolveName);
                }
                else
                {
                    log.WriteLine("    Error: Dependent assembly not supplied: " + resolveName);
                }

                return null;
            };
        }

        /// <summary>
        /// Attempts a reflection-only load of a dependent assembly, logging the error if the load fails.
        /// </summary>
        /// <param name="assemblyPath">Path of the assembly file to laod.</param>
        /// <returns>Loaded assembly, or null if the load failed.</returns>
        private static Assembly TryLoadDependentAssembly(string assemblyPath)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            }
            catch (IOException ex)
            {
                log.WriteLine("    Error: Failed to load dependent assembly: {0}. {1}", assemblyPath, ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                log.WriteLine("    Error: Failed to load dependent assembly: {0}. {1}", assemblyPath, ex.Message);
            }
            catch (SecurityException ex)
            {
                log.WriteLine("    Error: Failed to load dependent assembly: {0}. {1}", assemblyPath, ex.Message);
            }

            return assembly;
        }

        /// <summary>
        /// Searches the types in the input assembly for a type that implements IEmbeddedUI.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private static string FindEmbeddedUIClass(string module)
        {
            log.WriteLine("Searching for an embedded UI class in {0}", Path.GetFileName(module));

            string uiClass = null;

            var assembly = Assembly.ReflectionOnlyLoadFrom(module);

            foreach (var type in assembly.GetExportedTypes())
            {
                if (!type.IsAbstract)
                {
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        if (interfaceType.FullName == "WixToolset.Dtf.WindowsInstaller.IEmbeddedUI")
                        {
                            if (uiClass == null)
                            {
                                uiClass = assembly.GetName().Name + "!" + type.FullName;
                            }
                            else
                            {
                                throw new ArgumentException("Multiple IEmbeddedUI implementations found.");
                            }
                        }
                    }
                }
            }

            return uiClass;
        }

        /// <summary>
        /// Reflects on an input CA module to locate custom action entry-points.
        /// </summary>
        /// <param name="module">Assembly module with CA entry-points.</param>
        /// <returns>Mapping from entry-point names to assembly!class.method paths.</returns>
        private static IDictionary<string, string> FindEntryPoints(string module)
        {
            log.WriteLine("Searching for custom action entry points " +
                "in {0}", Path.GetFileName(module));

            var entryPoints = new Dictionary<string, string>();

            var assembly = Assembly.ReflectionOnlyLoadFrom(module);

            foreach (var type in assembly.GetExportedTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var entryPointName = MakeSfxCA.GetEntryPoint(method);
                    if (entryPointName != null)
                    {
                        var entryPointPath = string.Format(
                            "{0}!{1}.{2}",
                            Path.GetFileNameWithoutExtension(module),
                            type.FullName,
                            method.Name);
                        entryPoints.Add(entryPointName, entryPointPath);

                        log.WriteLine("    {0}={1}", entryPointName, entryPointPath);
                    }
                }
            }

            return entryPoints;
        }

        /// <summary>
        /// Check for a CustomActionAttribute and return the entrypoint name for the method if it is a CA method.
        /// </summary>
        /// <param name="method">A public static method.</param>
        /// <returns>Entrypoint name for the method as specified by the custom action attribute or just the method name,
        /// or null if the method is not a custom action method.</returns>
        private static string GetEntryPoint(MethodInfo method)
        {
            IList<CustomAttributeData> attributes;
            try
            {
                attributes = CustomAttributeData.GetCustomAttributes(method);
            }
            catch (FileLoadException)
            {
                // Already logged load failures in the assembly-resolve-handler.
                return null;
            }

            foreach (CustomAttributeData attribute in attributes)
            {
                if (attribute.ToString().StartsWith(
                    "[WixToolset.Dtf.WindowsInstaller.CustomActionAttribute(",
                    StringComparison.Ordinal))
                {
                    string entryPointName = null;
                    foreach (var argument in attribute.ConstructorArguments)
                    {
                        // The entry point name is the first positional argument, if specified.
                        entryPointName = (string) argument.Value;
                        break;
                    }

                    if (string.IsNullOrEmpty(entryPointName))
                    {
                        entryPointName = method.Name;
                    }

                    return entryPointName;
                }
            }

            return null;
        }

        /// <summary>
        /// Counts the number of template entrypoints in SfxCA.dll.
        /// </summary>
        /// <remarks>
        /// Depending on the requirements, SfxCA.dll might be built with
        /// more entrypoints than the default.
        /// </remarks>
        private static int GetEntryPointSlotCount(byte[] fileBytes, string entryPointFormat)
        {
            for (var count = 0; ; count++)
            {
                var templateName = string.Format(entryPointFormat, count);
                var templateAsciiBytes = Encoding.ASCII.GetBytes(templateName);

                var nameOffset = FindBytes(fileBytes, templateAsciiBytes);
                if (nameOffset < 0)
                {
                    return count;
                }
            }
        }

        /// <summary>
        /// Writes a modified version of SfxCA.dll to the output stream,
        /// with the template entry-points mapped to the CA entry-points.
        /// </summary>
        /// <remarks>
        /// To avoid having to recompile SfxCA.dll for every different set of CAs,
        /// this method looks for a preset number of template entry-points in the
        /// binary file and overwrites their entrypoint name and string data with
        /// CA-specific values.
        /// </remarks>
        private static void WriteEntryModule(
            string sfxDll, Stream outputStream, IDictionary<string, string> entryPoints, string uiClass)
        {
            log.WriteLine("Modifying SfxCA.dll stub");

            byte[] fileBytes;
            using (var readStream = File.OpenRead(sfxDll))
            {
                fileBytes = new byte[(int) readStream.Length];
                readStream.Read(fileBytes, 0, fileBytes.Length);
            }

            const string ENTRYPOINT_FORMAT = "CustomActionEntryPoint{0:d03}";
            const int MAX_ENTRYPOINT_NAME = 72;
            const int MAX_ENTRYPOINT_PATH = 160;
            //var emptyBytes = new byte[0];

            var slotCount = MakeSfxCA.GetEntryPointSlotCount(fileBytes, ENTRYPOINT_FORMAT);

            if (slotCount == 0)
            {
                throw new ArgumentException("Invalid SfxCA.dll file.");
            }

            if (entryPoints.Count > slotCount)
            {
                throw new ArgumentException(string.Format(
                    "The custom action assembly has {0} entrypoints, which is more than the maximum ({1}). " +
                    "Refactor the custom actions or add more entrypoint slots in SfxCA\\EntryPoints.h.",
                    entryPoints.Count, slotCount));
            }

            var slotSort = new string[slotCount];
            for (var i = 0; i < slotCount - entryPoints.Count; i++)
            {
                slotSort[i] = string.Empty;
            }

            entryPoints.Keys.CopyTo(slotSort, slotCount - entryPoints.Count);
            Array.Sort<string>(slotSort, slotCount - entryPoints.Count, entryPoints.Count, StringComparer.Ordinal);

            for (var i = 0; ; i++)
            {
                var templateName = string.Format(ENTRYPOINT_FORMAT, i);
                var templateAsciiBytes = Encoding.ASCII.GetBytes(templateName);
                var templateUniBytes = Encoding.Unicode.GetBytes(templateName);

                var nameOffset = MakeSfxCA.FindBytes(fileBytes, templateAsciiBytes);
                if (nameOffset < 0)
                {
                    break;
                }

                var pathOffset = MakeSfxCA.FindBytes(fileBytes, templateUniBytes);
                if (pathOffset < 0)
                {
                    break;
                }

                var entryPointName = slotSort[i];
                var entryPointPath = entryPointName.Length > 0 ?
                    entryPoints[entryPointName] : string.Empty;

                if (entryPointName.Length > MAX_ENTRYPOINT_NAME)
                {
                    throw new ArgumentException(string.Format(
                        "Entry point name exceeds limit of {0} characters: {1}",
                        MAX_ENTRYPOINT_NAME,
                        entryPointName));
                }

                if (entryPointPath.Length > MAX_ENTRYPOINT_PATH)
                {
                    throw new ArgumentException(string.Format(
                        "Entry point path exceeds limit of {0} characters: {1}",
                        MAX_ENTRYPOINT_PATH,
                        entryPointPath));
                }

                var replaceNameBytes = Encoding.ASCII.GetBytes(entryPointName);
                var replacePathBytes = Encoding.Unicode.GetBytes(entryPointPath);

                MakeSfxCA.ReplaceBytes(fileBytes, nameOffset, MAX_ENTRYPOINT_NAME, replaceNameBytes);
                MakeSfxCA.ReplaceBytes(fileBytes, pathOffset, MAX_ENTRYPOINT_PATH * 2, replacePathBytes);
            }

            if (entryPoints.Count == 0 && uiClass != null)
            {
                // Remove the zzz prefix from exported EmbeddedUI entry-points.
                foreach (var export in new string[] { "InitializeEmbeddedUI", "EmbeddedUIHandler", "ShutdownEmbeddedUI" })
                {
                    var exportNameBytes = Encoding.ASCII.GetBytes("zzz" + export);

                    var exportOffset = MakeSfxCA.FindBytes(fileBytes, exportNameBytes);
                    if (exportOffset < 0)
                    {
                        throw new ArgumentException("Input SfxCA.dll does not contain exported entry-point: " + export);
                    }

                    var replaceNameBytes = Encoding.ASCII.GetBytes(export);
                    MakeSfxCA.ReplaceBytes(fileBytes, exportOffset, exportNameBytes.Length, replaceNameBytes);
                }

                if (uiClass.Length > MAX_ENTRYPOINT_PATH)
                {
                    throw new ArgumentException(string.Format(
                        "UI class full name exceeds limit of {0} characters: {1}",
                        MAX_ENTRYPOINT_PATH,
                        uiClass));
                }

                var templateBytes = Encoding.Unicode.GetBytes("InitializeEmbeddedUI_FullClassName");
                var replaceBytes = Encoding.Unicode.GetBytes(uiClass);

                // Fill in the embedded UI implementor class so the proxy knows which one to load.
                var replaceOffset = MakeSfxCA.FindBytes(fileBytes, templateBytes);
                if (replaceOffset >= 0)
                {
                    MakeSfxCA.ReplaceBytes(fileBytes, replaceOffset, MAX_ENTRYPOINT_PATH * 2, replaceBytes);
                }
            }

            outputStream.Write(fileBytes, 0, fileBytes.Length);
        }

        /// <summary>
        /// Searches for a sub-array of bytes within a larger array of bytes.
        /// </summary>
        private static int FindBytes(byte[] source, byte[] find)
        {
            for (var i = 0; i < source.Length; i++)
            {
                int j;
                for (j = 0; j < find.Length; j++)
                {
                    if (source[i + j] != find[j])
                    {
                        break;
                    }
                }

                if (j == find.Length)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Replaces a range of bytes with new bytes, padding any extra part
        /// of the range with zeroes.
        /// </summary>
        private static void ReplaceBytes(
            byte[] source, int offset, int length, byte[] replace)
        {
            for (var i = 0; i < length; i++)
            {
                if (i < replace.Length)
                {
                    source[offset + i] = replace[i];
                }
                else
                {
                    source[offset + i] = 0;
                }
            }
        }

        /// <summary>
        /// Print the name of one file as it is being packed into the cab.
        /// </summary>
        private static void PackProgress(object source, ArchiveProgressEventArgs e)
        {
            if (e.ProgressType == ArchiveProgressType.StartFile && log != null)
            {
                log.WriteLine("    {0}", e.CurrentFileName);
            }
        }

        /// <summary>
        /// Gets a mapping from filenames as they will be in the cab to filenames
        /// as they are currently on disk.
        /// </summary>
        /// <remarks>
        /// By default, all files will be placed in the root of the cab. But inputs may
        /// optionally include an alternate inside-cab file path before an equals sign.
        /// </remarks>
        private static IDictionary<string, string> GetPackFileMap(IList<string> inputs)
        {
            var fileMap = new Dictionary<string, string>();
            foreach (var inputFile in inputs)
            {
                if (inputFile.IndexOf('=') > 0)
                {
                    var parse = inputFile.Split('=');
                    if (!fileMap.ContainsKey(parse[0]))
                    {
                        fileMap.Add(parse[0], parse[1]);
                    }
                }
                else
                {
                    var fileName = Path.GetFileName(inputFile);
                    if (!fileMap.ContainsKey(fileName))
                    {
                        fileMap.Add(fileName, inputFile);
                    }
                }
            }
            return fileMap;
        }

        /// <summary>
        /// Packs the input files into a cab that is appended to the
        /// output SfxCA.dll.
        /// </summary>
        private static void PackInputFiles(string outputFile, IDictionary<string, string> fileMap)
        {
            log.WriteLine("Packaging files");

            var cabInfo = new CabInfo(outputFile);
            cabInfo.PackFileSet(null, fileMap, CompressionLevel.Max, PackProgress);
        }

        /// <summary>
        /// Copies the version resource information from the CA module to
        /// the CA package. This gives the package the file version and
        /// description of the CA module, instead of the version and
        /// description of SfxCA.dll.
        /// </summary>
        private static void CopyVersionResource(string sourceFile, string destFile)
        {
            log.WriteLine("Copying file version info from {0} to {1}",
                sourceFile, destFile);

            var rc = new ResourceCollection();
            rc.Find(sourceFile, ResourceType.Version);
            rc.Load(sourceFile);
            rc.Save(destFile);
        }
    }
}
