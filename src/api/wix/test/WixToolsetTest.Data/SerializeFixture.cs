// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller.Rows;
    using Xunit;

    using Wid = WixToolset.Data.WindowsInstaller;

    public class SerializeFixture
    {
        [Fact]
        public void CanSaveAndLoadIntermediate()
        {
            var sln = new SourceLineNumber("test.wxs", 1);

            var section = new IntermediateSection("test", SectionType.Package);

            section.AddSymbol(new ComponentSymbol(sln, new Identifier(AccessModifier.Global, "TestComponent"))
            {
                ComponentId = String.Empty,
                DirectoryRef = "TestFolder",
                Location = ComponentLocation.Either,
                KeyPath = null,
            });

            section.AddSymbol(new DirectorySymbol(sln, new Identifier(AccessModifier.Virtual, "TestFolder"))
            {
                ParentDirectoryRef = String.Empty,
                Name = "Test Folder",
            });

            var intermediate = new Intermediate("TestIntermediate", IntermediateLevels.Compiled, new[] { section }, null);

            intermediate.UpdateLevel(IntermediateLevels.Linked);
            intermediate.UpdateLevel(IntermediateLevels.Resolved);

            var path = Path.GetTempFileName();
            try
            {
                intermediate.Save(path);

                var loaded = Intermediate.Load(path);

                Assert.True(loaded.HasLevel(IntermediateLevels.Compiled));
                Assert.True(loaded.HasLevel(IntermediateLevels.Linked));
                Assert.True(loaded.HasLevel(IntermediateLevels.Resolved));

                var componentSymbol = loaded.Sections.Single().Symbols.OfType<ComponentSymbol>().Single();

                Assert.Equal("TestComponent", componentSymbol.Id.Id);
                Assert.Equal(AccessModifier.Global, componentSymbol.Id.Access);
                Assert.Equal(String.Empty, componentSymbol.ComponentId);
                Assert.Equal("TestFolder", componentSymbol.DirectoryRef);
                Assert.Equal(ComponentLocation.Either, componentSymbol.Location);
                Assert.Null(componentSymbol.KeyPath);

                var directorySymbol = loaded.Sections.Single().Symbols.OfType<DirectorySymbol>().Single();

                Assert.Equal("TestFolder", directorySymbol.Id.Id);
                Assert.Equal(AccessModifier.Virtual, directorySymbol.Id.Access);
                Assert.Equal(String.Empty, directorySymbol.ParentDirectoryRef);
                Assert.Equal("Test Folder", directorySymbol.Name);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CanUpdateIntermediate()
        {
            var sln = new SourceLineNumber("test.wxs", 1);
            var section = new IntermediateSection("test", SectionType.Package);

            section.AddSymbol(new ComponentSymbol(sln, new Identifier(AccessModifier.Global, "TestComponent"))
            {
                ComponentId = new Guid(1, 0, 0, new byte[8]).ToString("B"),
                DirectoryRef = "TestFolder",
                Location = ComponentLocation.Either,
            });

            var intermediate = new Intermediate("TestIntermediate", IntermediateLevels.Compiled, new[] { section }, null);

            var path = Path.GetTempFileName();
            try
            {
                intermediate.Save(path);

                var uri = new Uri(Path.GetFullPath(path));
                var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);

                using (var wixout = WixOutput.Read(uri, stream))
                {
                    var loaded = Intermediate.Load(wixout);
                    var symbol = (ComponentSymbol)loaded.Sections.Single().Symbols.Single();

                    Assert.Equal("TestComponent", symbol.Id.Id);
                    Assert.Equal(AccessModifier.Global, symbol.Id.Access);

                    wixout.Reopen(writable: true);

                    section.AddSymbol(new ComponentSymbol(sln, new Identifier(AccessModifier.Global, "NewComponent"))
                    {
                        ComponentId = new Guid(1, 0, 0, new byte[8]).ToString("B"),
                    });

                    intermediate.Save(wixout);
                    loaded = Intermediate.Load(wixout);

                    var newSymbol = loaded.Sections.Single().Symbols.Where(t => t.Id.Id == "NewComponent");
                    Assert.Single(newSymbol);
                }

                var loadedAfterDispose = Intermediate.Load(path);
                var newSymbolStillThere = loadedAfterDispose.Sections.Single().Symbols.Where(t => t.Id.Id == "NewComponent");
                Assert.Single(newSymbolStillThere);

            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CanSaveAndLoadIntermediateWithCustomDefinitions()
        {
            var sln = new SourceLineNumber("test.wxs", 1);

            var section = new IntermediateSection("test", SectionType.Package);

            var fieldDefs = new[]
            {
                new IntermediateFieldDefinition("A", IntermediateFieldType.String),
                new IntermediateFieldDefinition("B", IntermediateFieldType.Number),
                new IntermediateFieldDefinition("C", IntermediateFieldType.Bool),
            };

            var symbolDef = new IntermediateSymbolDefinition("CustomDef2", fieldDefs, null);

            var symbol = symbolDef.CreateSymbol(sln, new Identifier(AccessModifier.Global, "customT"));
            symbol.Set(0, "foo");
            symbol.Set(1, 2);
            symbol.Set(2, true);

            section.AddSymbol(symbol);

            var intermediate = new Intermediate("TestIntermediate", new[] { section }, null);

            var path = Path.GetTempFileName();
            try
            {
                intermediate.Save(path);

                var loaded = Intermediate.Load(path);
                var loadedSection = loaded.Sections.Single();
                var loadedSymbol = loadedSection.Symbols.Single();

                Assert.Equal("foo", loadedSymbol.AsString(0));
                Assert.Equal(2, loadedSymbol[1].AsNumber());
                Assert.True(loadedSymbol[2].AsBool());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CanSaveAndLoadMultipleIntermediateWithCustomDefinitions()
        {
            var sln = new SourceLineNumber("test.wxs", 1);

            // Intermediate #1
            var fieldDefs = new[]
            {
                new IntermediateFieldDefinition("A", IntermediateFieldType.String),
                new IntermediateFieldDefinition("B", IntermediateFieldType.Number),
                new IntermediateFieldDefinition("C", IntermediateFieldType.Bool),
            };

            var symbolDef = new IntermediateSymbolDefinition("CustomDef", fieldDefs, null);

            var symbol = symbolDef.CreateSymbol(sln, new Identifier(AccessModifier.Global, "customT"));
            symbol.Set(0, "foo");
            symbol.Set(1, 2);
            symbol.Set(2, true);

            var section = new IntermediateSection("test", SectionType.Package);
            section.AddSymbol(symbol);

            var intermediate1 = new Intermediate("TestIntermediate", new[] { section }, null);

            // Intermediate #2
            var fieldDefs2 = new[]
            {
                new IntermediateFieldDefinition("A", IntermediateFieldType.String),
                new IntermediateFieldDefinition("B", IntermediateFieldType.Number),
                new IntermediateFieldDefinition("C", IntermediateFieldType.Bool),
                new IntermediateFieldDefinition("D", IntermediateFieldType.String),
            };

            var symbolDef2 = new IntermediateSymbolDefinition("CustomDef2", 1, fieldDefs2, null);

            var symbol2 = symbolDef2.CreateSymbol(sln, new Identifier(AccessModifier.Global, "customT2"));
            symbol2.Set(0, "bar");
            symbol2.Set(1, 3);
            symbol2.Set(2, false);
            symbol2.Set(3, "baz");

            var section2 = new IntermediateSection("test2", SectionType.Fragment);
            section2.AddSymbol(symbol2);

            var intermediate2 = new Intermediate("TestIntermediate2", new[] { section2 }, null);

            // Save
            var path1 = Path.GetTempFileName();
            var path2 = Path.GetTempFileName();
            try
            {
                intermediate1.Save(path1);
                intermediate2.Save(path2);

                var loaded = Intermediate.Load(new[] { path1, path2 });

                var loaded1 = loaded.First();
                var loaded2 = loaded.Skip(1).Single();

                var loadedSymbol1 = loaded1.Sections.Single().Symbols.Single();
                var loadedSymbol2 = loaded2.Sections.Single().Symbols.Single();

                Assert.Equal("foo", loadedSymbol1.AsString(0));
                Assert.Equal(2, loadedSymbol1[1].AsNumber());
                Assert.True(loadedSymbol1[2].AsBool());

                Assert.Equal("bar", loadedSymbol2.AsString(0));
                Assert.Equal(3, loadedSymbol2[1].AsNumber());
                Assert.False(loadedSymbol2[2].AsBool());
                Assert.Equal("baz", loadedSymbol2.AsString(3));
            }
            finally
            {
                File.Delete(path2);
                File.Delete(path1);
            }
        }

        [Fact]
        public void CanSaveAndLoadMultipleIntermediateWithCustomDefinitionsAndTags()
        {
            var sln = new SourceLineNumber("test.wxs", 1);

            // Intermediate #1
            var fieldDefs = new[]
            {
                new IntermediateFieldDefinition("A", IntermediateFieldType.String),
                new IntermediateFieldDefinition("B", IntermediateFieldType.Number),
                new IntermediateFieldDefinition("C", IntermediateFieldType.Bool),
            };

            var symbolDef = new IntermediateSymbolDefinition("CustomDef", fieldDefs, null);

            symbolDef.AddTag("customDef");

            var symbol = symbolDef.CreateSymbol(sln, new Identifier(AccessModifier.Global, "customT"));
            symbol.Set(0, "foo");
            symbol.Set(1, 2);
            symbol.Set(2, true);

            symbol.AddTag("symbol1tag");

            var section = new IntermediateSection("test", SectionType.Package);
            section.AddSymbol(symbol);

            var intermediate1 = new Intermediate("TestIntermediate", new[] { section }, null);

            // Intermediate #2
            var fieldDefs2 = new[]
            {
                new IntermediateFieldDefinition("A", IntermediateFieldType.String),
                new IntermediateFieldDefinition("B", IntermediateFieldType.Number),
                new IntermediateFieldDefinition("C", IntermediateFieldType.Bool),
                new IntermediateFieldDefinition("D", IntermediateFieldType.String),
            };

            var symbolDef2 = new IntermediateSymbolDefinition("CustomDef2", 1, fieldDefs2, null);

            symbolDef2.AddTag("customDef2");
            symbolDef2.AddTag("customDef2 tag2");

            var symbol2 = symbolDef2.CreateSymbol(sln, new Identifier(AccessModifier.Global, "customT2"));
            symbol2.Set(0, "bar");
            symbol2.Set(1, 3);
            symbol2.Set(2, false);
            symbol2.Set(3, "baz");

            symbol2.AddTag("symbol2tag1");
            symbol2.AddTag("symbol2tag2");

            var section2 = new IntermediateSection("test2", SectionType.Fragment);
            section2.AddSymbol(symbol2);

            var intermediate2 = new Intermediate("TestIntermediate2", new[] { section2 }, null);

            // Save
            var path1 = Path.GetTempFileName();
            var path2 = Path.GetTempFileName();
            try
            {
                intermediate1.Save(path1);
                intermediate2.Save(path2);

                var loaded = Intermediate.Load(new[] { path1, path2 });

                var loaded1 = loaded.First();
                var loaded2 = loaded.Skip(1).Single();

                var loadedSymbol1 = loaded1.Sections.Single().Symbols.Single();
                var loadedSymbol2 = loaded2.Sections.Single().Symbols.Single();

                Assert.True(loadedSymbol1.Definition.HasTag("customDef"));
                Assert.Equal("foo", loadedSymbol1.AsString(0));
                Assert.Equal(2, loadedSymbol1[1].AsNumber());
                Assert.True(loadedSymbol1[2].AsBool());
                Assert.True(loadedSymbol1.HasTag("symbol1tag"));

                Assert.True(loadedSymbol2.Definition.HasTag("customDef2"));
                Assert.True(loadedSymbol2.Definition.HasTag("customDef2 tag2"));
                Assert.Equal("bar", loadedSymbol2.AsString(0));
                Assert.Equal(3, loadedSymbol2[1].AsNumber());
                Assert.False(loadedSymbol2[2].AsBool());
                Assert.Equal("baz", loadedSymbol2.AsString(3));
                Assert.True(loadedSymbol2.HasTag("symbol2tag1"));
                Assert.True(loadedSymbol2.HasTag("symbol2tag2"));
            }
            finally
            {
                File.Delete(path2);
                File.Delete(path1);
            }
        }

        [Fact]
        public void CanSaveAndLoadIntermediateWithLocalization()
        {
            var sln = new SourceLineNumber("test.wxs", 10);

            var bindVariables = new[]
            {
                new BindVariable { Id = "TestVar1", Value = "TestValue1", SourceLineNumbers = sln },
                new BindVariable { Id = "TestVar2", Value = "TestValue2", Overridable = true, SourceLineNumbers = sln },
            };

            var controls = new[]
            {
                new LocalizedControl("TestDlg1", "TestControl1", 10, 10, 100, 100, false, false, false, null),
                new LocalizedControl("TestDlg1", "TestControl2", 100, 90, 50, 70, false, false, false, "localized"),
            };

            var localizations = new[]
            {
                new Localization(65001, 1252, null, bindVariables.ToDictionary(b => b.Id), controls.ToDictionary(c => c.GetKey()))
            };

            var section = new IntermediateSection("test", SectionType.Package);

            section.AddSymbol(new ComponentSymbol(sln, new Identifier(AccessModifier.Global, "TestComponent"))
            {
                ComponentId = new Guid(1, 0, 0, new byte[8]).ToString("B"),
                DirectoryRef = "TestFolder",
                Location = ComponentLocation.Either,
            });

            var intermediate = new Intermediate("TestIntermediate", new[] { section }, localizations.ToDictionary(l => l.Culture));

            var path = Path.GetTempFileName();
            try
            {
                intermediate.Save(path);

                var loaded = Intermediate.Load(path);

                var loc = loaded.Localizations.Single();
                Assert.Equal(65001, loc.Codepage);
                Assert.Empty(loc.Culture);
                WixAssert.CompareLineByLine(new[]
                {
                    "TestVar1/TestValue1/False",
                    "TestVar2/TestValue2/True",
                }, loc.Variables.Select(v => String.Join("/", v.Id, v.Value, v.Overridable)).ToArray());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CanSaveAndLoadWindowsInstallerData()
        {
            var sln = new SourceLineNumber("test.wxs", 1);
            var windowsInstallerData = new Wid.WindowsInstallerData(sln)
            {
                Type = OutputType.Package,
            };

            var fileTable = windowsInstallerData.EnsureTable(Wid.WindowsInstallerTableDefinitions.File);
            var fileRow = (FileRow)fileTable.CreateRow(sln);
            fileRow.File = "TestFile";

            var path = Path.GetTempFileName();
            try
            {
                using (var wixout = WixOutput.Create(path))
                {
                    windowsInstallerData.Save(wixout);
                }

                var loaded = Wid.WindowsInstallerData.Load(path);

                var loadedTable = Assert.Single(loaded.Tables);
                Assert.Equal(Wid.WindowsInstallerTableDefinitions.File.Name, loadedTable.Name);

                var loadedRow = Assert.Single(loadedTable.Rows);
                var loadedFileRow = Assert.IsType<FileRow>(loadedRow);

                Assert.Equal("TestFile", loadedFileRow.File);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
