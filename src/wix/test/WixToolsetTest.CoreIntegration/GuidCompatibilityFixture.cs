// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class GuidCompatibilityFixture
    {
        [Fact]
        public void VerifyModernX86GuidCompatibility()
        {
            var folder = TestData.Get("TestData", "GuidCompatibility");

            using var fs = new DisposableFileSystem();

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "Package.wxs"),
                Path.Combine(folder, "Components.wxs"),
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(baseFolder, "bin", "test.msi")
            });

            result.AssertSuccess();

            var wixpdb = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
            var section = wixpdb.Sections.Single();

            var symbols = section.Symbols.OfType<ComponentSymbol>().ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileFoo {AD87A3DE-042B-5CB1-82C8-782626737A3B} File:filrCrmOAxegUXdKLQjj8mKJFZC2AY",
                "RegHkcr {246B5173-0924-511B-8828-77DD3BE87C7D} Registry:regYqKbt2o5W7y7CIBKba5Du9MJ2xU",
                "RegHkcu {663BDBDA-FAAF-5B7A-B8BA-D1AB3B5284CD} Registry:regCDVjmO6qHBvQzdqM8XyBiEUuf48",
                "RegHklm {6A5C002D-C0C9-5B03-B883-BE3DF3FA21A6} Registry:regPKV64nTdbwlRm8bkU8k4Kxj6Km8",
                "RegHkmu {529D5485-ED3E-5D1C-8517-A32103A08F76} Registry:regOPf4kOF1RLKsRy9oG4MP1Rqm8JY",
                "RegHku {0C78D91F-7913-5747-918C-322DA2A15823} Registry:regJHg773M8wPDSk6j7CYRThPX7uOw"
            }, symbols.Select(s => $"{s.Id.Id} {s.ComponentId} {s.KeyPathType}:{s.KeyPath}").OrderBy(s => s).ToArray());
        }

        [Fact]
        public void VerifyModernX64GuidCompatibility()
        {
            var folder = TestData.Get("TestData", "GuidCompatibility");

            using var fs = new DisposableFileSystem();

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "Package.wxs"),
                Path.Combine(folder, "Components.wxs"),
                "-arch", "x64",
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(baseFolder, "bin", "test.msi")
            });

            result.AssertSuccess();

            var wixpdb = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
            var section = wixpdb.Sections.Single();

            var symbols = section.Symbols.OfType<ComponentSymbol>().ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileFoo {AD87A3DE-042B-5CB1-82C8-782626737A3B} File:filrCrmOAxegUXdKLQjj8mKJFZC2AY",
                "RegHkcr {A2D16D0D-8856-5B34-804F-64E0EE691ACD} Registry:regYqKbt2o5W7y7CIBKba5Du9MJ2xU",
                "RegHkcu {CE5B9399-8F69-50E4-AE76-9F5C3F5A4D72} Registry:regCDVjmO6qHBvQzdqM8XyBiEUuf48",
                "RegHklm {45973AE0-BFEF-5A88-AC82-416C9699ED23} Registry:regPKV64nTdbwlRm8bkU8k4Kxj6Km8",
                "RegHkmu {841288A2-7520-5CA5-8CA0-4B4711D3F669} Registry:regOPf4kOF1RLKsRy9oG4MP1Rqm8JY",
                "RegHku {0AC9C5B2-E60A-5CD9-9A4E-B5DEABAD1D14} Registry:regJHg773M8wPDSk6j7CYRThPX7uOw"
            }, symbols.Select(s => $"{s.Id.Id} {s.ComponentId} {s.KeyPathType}:{s.KeyPath}").OrderBy(s => s).ToArray());
        }

        [Fact]
        public void VerifyModernArm64GuidCompatibility()
        {
            var folder = TestData.Get("TestData", "GuidCompatibility");

            using var fs = new DisposableFileSystem();

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "Package.wxs"),
                Path.Combine(folder, "Components.wxs"),
                "-arch", "arm64",
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(baseFolder, "bin", "test.msi")
            });

            result.AssertSuccess();

            var wixpdb = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
            var section = wixpdb.Sections.Single();

            var symbols = section.Symbols.OfType<ComponentSymbol>().ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileFoo {AD87A3DE-042B-5CB1-82C8-782626737A3B} File:filrCrmOAxegUXdKLQjj8mKJFZC2AY",
                "RegHkcr {A2D16D0D-8856-5B34-804F-64E0EE691ACD} Registry:regYqKbt2o5W7y7CIBKba5Du9MJ2xU",
                "RegHkcu {CE5B9399-8F69-50E4-AE76-9F5C3F5A4D72} Registry:regCDVjmO6qHBvQzdqM8XyBiEUuf48",
                "RegHklm {45973AE0-BFEF-5A88-AC82-416C9699ED23} Registry:regPKV64nTdbwlRm8bkU8k4Kxj6Km8",
                "RegHkmu {841288A2-7520-5CA5-8CA0-4B4711D3F669} Registry:regOPf4kOF1RLKsRy9oG4MP1Rqm8JY",
                "RegHku {0AC9C5B2-E60A-5CD9-9A4E-B5DEABAD1D14} Registry:regJHg773M8wPDSk6j7CYRThPX7uOw"
            }, symbols.Select(s => $"{s.Id.Id} {s.ComponentId} {s.KeyPathType}:{s.KeyPath}").OrderBy(s => s).ToArray());
        }

        [Fact]
        public void VerifyWix3X86GuidCompatibility()
        {
            var folder = TestData.Get("TestData", "GuidCompatibility");

            using var fs = new DisposableFileSystem();

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "Package.wxs"),
                Path.Combine(folder, "Components.wxs"),
                "-bcgg",
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(baseFolder, "bin", "test.msi")
            });

            result.AssertSuccess();

            var wixpdb = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
            var section = wixpdb.Sections.Single();

            var symbols = section.Symbols.OfType<ComponentSymbol>().ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileFoo {AD87A3DE-042B-5CB1-82C8-782626737A3B} File:filrCrmOAxegUXdKLQjj8mKJFZC2AY",
                "RegHkcr {656550C1-E3C3-5943-900A-A286A24F1F7C} Registry:regYqKbt2o5W7y7CIBKba5Du9MJ2xU",
                "RegHkcu {5C7CBEDE-4C14-58AF-8365-59C602A20543} Registry:regCDVjmO6qHBvQzdqM8XyBiEUuf48",
                "RegHklm {186E35F4-96E9-5DE4-8F7B-73EA096A8543} Registry:regPKV64nTdbwlRm8bkU8k4Kxj6Km8",
                "RegHkmu {C1707072-2C2C-5249-85E1-37418A31DF2D} Registry:regOPf4kOF1RLKsRy9oG4MP1Rqm8JY",
                "RegHku {76A0D3F4-7BD5-51DE-8FDA-6C30674083CB} Registry:regJHg773M8wPDSk6j7CYRThPX7uOw"
            }, symbols.Select(s => $"{s.Id.Id} {s.ComponentId} {s.KeyPathType}:{s.KeyPath}").OrderBy(s => s).ToArray());
        }

        [Fact]
        public void VerifyWix3X64GuidCompatibility()
        {
            var folder = TestData.Get("TestData", "GuidCompatibility");

            using var fs = new DisposableFileSystem();

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "Package.wxs"),
                Path.Combine(folder, "Components.wxs"),
                "-bcgg",
                "-arch", "x64",
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(baseFolder, "bin", "test.msi")
            });

            result.AssertSuccess();

            var wixpdb = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
            var section = wixpdb.Sections.Single();

            var symbols = section.Symbols.OfType<ComponentSymbol>().ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileFoo {AD87A3DE-042B-5CB1-82C8-782626737A3B} File:filrCrmOAxegUXdKLQjj8mKJFZC2AY",
                "RegHkcr {E420B3FC-4889-5D2B-8E2D-DC8D05B62B5B} Registry:regYqKbt2o5W7y7CIBKba5Du9MJ2xU",
                "RegHkcu {12D81E73-4429-5D97-8318-7E719F660847} Registry:regCDVjmO6qHBvQzdqM8XyBiEUuf48",
                "RegHklm {2E7AA7BE-006C-5FFE-A73F-1C65142AB3D7} Registry:regPKV64nTdbwlRm8bkU8k4Kxj6Km8",
                "RegHkmu {C7972BDE-CA76-564D-A9BA-FD3A0B930DCF} Registry:regOPf4kOF1RLKsRy9oG4MP1Rqm8JY",
                "RegHku {AB4A2008-03FC-513E-8B92-35CE837C212E} Registry:regJHg773M8wPDSk6j7CYRThPX7uOw"
            }, symbols.Select(s => $"{s.Id.Id} {s.ComponentId} {s.KeyPathType}:{s.KeyPath}").OrderBy(s => s).ToArray());
        }

        [Fact]
        public void VerifyWix3Arm64GuidCompatibility()
        {
            var folder = TestData.Get("TestData", "GuidCompatibility");

            using var fs = new DisposableFileSystem();

            var baseFolder = fs.GetFolder();
            var intermediateFolder = Path.Combine(baseFolder, "obj");

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "Package.wxs"),
                Path.Combine(folder, "Components.wxs"),
                "-bcgg",
                "-arch", "x64",
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(baseFolder, "bin", "test.msi")
            });

            result.AssertSuccess();

            var wixpdb = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
            var section = wixpdb.Sections.Single();

            var symbols = section.Symbols.OfType<ComponentSymbol>().ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileFoo {AD87A3DE-042B-5CB1-82C8-782626737A3B} File:filrCrmOAxegUXdKLQjj8mKJFZC2AY",
                "RegHkcr {E420B3FC-4889-5D2B-8E2D-DC8D05B62B5B} Registry:regYqKbt2o5W7y7CIBKba5Du9MJ2xU",
                "RegHkcu {12D81E73-4429-5D97-8318-7E719F660847} Registry:regCDVjmO6qHBvQzdqM8XyBiEUuf48",
                "RegHklm {2E7AA7BE-006C-5FFE-A73F-1C65142AB3D7} Registry:regPKV64nTdbwlRm8bkU8k4Kxj6Km8",
                "RegHkmu {C7972BDE-CA76-564D-A9BA-FD3A0B930DCF} Registry:regOPf4kOF1RLKsRy9oG4MP1Rqm8JY",
                "RegHku {AB4A2008-03FC-513E-8B92-35CE837C212E} Registry:regJHg773M8wPDSk6j7CYRThPX7uOw"
            }, symbols.Select(s => $"{s.Id.Id} {s.ComponentId} {s.KeyPathType}:{s.KeyPath}").OrderBy(s => s).ToArray());
        }
    }
}
