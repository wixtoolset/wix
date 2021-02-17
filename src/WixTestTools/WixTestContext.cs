// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Win32;
    using WixBuildTools.TestSupport;
    using Xunit.Abstractions;

    public class WixTestContext
    {
        static readonly string RootDataPath = Path.GetFullPath(TestData.Get("TestData"));

        public WixTestContext(ITestOutputHelper testOutputHelper)
        {
            var test = GetTest(testOutputHelper);
            var splitClassName = test.TestCase.TestMethod.TestClass.Class.Name.Split('.');

            this.TestGroupName = splitClassName.Last();
            this.TestName = test.TestCase.TestMethod.Method.Name;

            this.TestDataFolder = Path.Combine(RootDataPath, this.TestGroupName);
        }

        public string TestDataFolder { get; }

        /// <summary>
        /// Gets the name of the current test group.
        /// </summary>
        public string TestGroupName { get; }

        public string TestName { get; }

        /// <summary>
        /// Gets the test install directory for the current test.
        /// </summary>
        /// <param name="additionalPath">Additional subdirectories under the test install directory.</param>
        /// <returns>Full path to the test install directory.</returns>
        /// <remarks>
        /// The package or bundle must install into [ProgramFilesFolder]\~Test WiX\[TestGroupName]\([Additional]).
        /// </remarks>
        public string GetTestInstallFolder(string additionalPath = null)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "~Test WiX", this.TestGroupName, additionalPath ?? String.Empty);
        }

        /// <summary>
        /// Gets the test registry key for the current test.
        /// </summary>
        /// <param name="additionalPath">Additional subkeys under the test registry key.</param>
        /// <returns>Full path to the test registry key.</returns>
        /// <remarks>
        /// The package must write into HKLM\Software\WiX\Tests\[TestGroupName]\([Additional]).
        /// </remarks>
        public RegistryKey GetTestRegistryRoot(string additionalPath = null)
        {
            var key = String.Format(@"Software\WOW6432Node\WiX\Tests\{0}\{1}", this.TestGroupName, additionalPath ?? String.Empty);
            return Registry.LocalMachine.OpenSubKey(key, true);
        }

        private static ITest GetTest(ITestOutputHelper output)
        {
            // https://github.com/xunit/xunit/issues/416#issuecomment-378512739
            var type = output.GetType();
            var testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            var test = (ITest)testMember.GetValue(output);
            return test;
        }
    }
}
