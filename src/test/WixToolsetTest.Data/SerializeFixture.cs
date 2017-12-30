// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using System;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class SerializeFixture
    {
        [Fact]
        public void CanSaveAndLoadIntermediate()
        {
            var sln = new SourceLineNumber("test.wxs", 1);

            var section = new IntermediateSection("test", SectionType.Product, 65001);

            section.Tuples.Add(new ComponentTuple(sln, new Identifier("TestComponent", AccessModifier.Public))
            {
                ComponentId = new Guid(1, 0, 0, new byte[8]).ToString("B"),
                Directory_ = "TestFolder",
                Attributes = 2,
            });

            var intermediate = new Intermediate("TestIntermediate", new[] { section }, null, null);

            var path = Path.GetTempFileName();
            intermediate.Save(path);

            var loaded = Intermediate.Load(path);

            var tuple = (ComponentTuple)loaded.Sections.Single().Tuples.Single();

            Assert.Equal("TestComponent", tuple.Id.Id);
            Assert.Equal(AccessModifier.Public, tuple.Id.Access);
            Assert.Equal("TestFolder", tuple.Directory_);
            Assert.Equal(2, tuple.Attributes);
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
                new LocalizedControl("TestDlg1", "TestControl1", 10, 10, 100, 100, 0, null),
                new LocalizedControl("TestDlg1", "TestControl2", 100, 90, 50, 70, 0, "localized"),
            };

            var localizations = new[]
            {
                new Localization(65001, null, bindVariables.ToDictionary(b => b.Id), controls.ToDictionary(c => c.GetKey()))
            };

            var section = new IntermediateSection("test", SectionType.Product, 65001);

            section.Tuples.Add(new ComponentTuple(sln, new Identifier("TestComponent", AccessModifier.Public))
            {
                ComponentId = new Guid(1, 0, 0, new byte[8]).ToString("B"),
                Directory_ = "TestFolder",
                Attributes = 2,
            });

            var intermediate = new Intermediate("TestIntermediate", new[] { section }, localizations.ToDictionary(l => l.Culture), null);

            var path = Path.GetTempFileName();
            try
            {
                intermediate.Save(path);

                var loaded = Intermediate.Load(path);

                var loc = loaded.Localizations.Single();
                Assert.Equal(65001, loc.Codepage);
                Assert.Empty(loc.Culture);
                Assert.Equal(new[]
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
    }
}
