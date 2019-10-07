// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using WixToolset.Data;
    using Xunit;

    public class TagFixture
    {
        [Fact]
        public void CanAddSingleTag()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test"));
            Assert.True(tuple.HasTag("test"));
        }

        [Fact]
        public void CanAddDuplicateTag()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test"));
            Assert.False(tuple.AddTag("test"));
        }

        [Fact]
        public void CanAddRemoveSingleTag()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test"));
            Assert.True(tuple.RemoveTag("test"));
            Assert.False(tuple.HasTag("test"));
        }

        [Fact]
        public void CanAddMultipleTags()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test1"));
            Assert.True(tuple.AddTag("test2"));
            Assert.True(tuple.HasTag("test1"));
            Assert.True(tuple.HasTag("test2"));
        }

        [Fact]
        public void CanAddRemoveMultipleTags()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test1"));
            Assert.True(tuple.AddTag("test2"));
            Assert.True(tuple.RemoveTag("test2"));
            Assert.False(tuple.HasTag("test2"));
            Assert.True(tuple.RemoveTag("test1"));
            Assert.False(tuple.HasTag("test1"));
        }

        [Fact]
        public void CanAddRemoveMissingTags()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test1"));
            Assert.True(tuple.AddTag("test2"));
            Assert.False(tuple.RemoveTag("test3"));
        }

        [Fact]
        public void CanAdd2AndRemoveAllTags()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test1"));
            Assert.True(tuple.AddTag("test2"));
            Assert.True(tuple.RemoveTag("test1"));
            Assert.True(tuple.RemoveTag("test2"));
        }

        [Fact]
        public void CanAdd3AndRemoveAllTags()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test1"));
            Assert.True(tuple.AddTag("test2"));
            Assert.True(tuple.AddTag("test3"));
            Assert.True(tuple.RemoveTag("test1"));
            Assert.True(tuple.RemoveTag("test3"));
            Assert.True(tuple.RemoveTag("test2"));
        }

        [Fact]
        public void CanAdd3AndRemoveMissingTags()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.True(tuple.AddTag("test1"));
            Assert.True(tuple.AddTag("test2"));
            Assert.True(tuple.AddTag("test3"));
            Assert.False(tuple.RemoveTag("test4"));
            Assert.True(tuple.RemoveTag("test1"));
            Assert.True(tuple.RemoveTag("test3"));
            Assert.True(tuple.RemoveTag("test2"));
        }
    }
}
