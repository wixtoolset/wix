// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using System;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
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
    }
}
