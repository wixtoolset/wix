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
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test"));
            Assert.True(symbol.HasTag("test"));
        }

        [Fact]
        public void CanAddDuplicateTag()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test"));
            Assert.False(symbol.AddTag("test"));
        }

        [Fact]
        public void CanAddRemoveSingleTag()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test"));
            Assert.True(symbol.RemoveTag("test"));
            Assert.False(symbol.HasTag("test"));
        }

        [Fact]
        public void CanAddMultipleTags()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test1"));
            Assert.True(symbol.AddTag("test2"));
            Assert.True(symbol.HasTag("test1"));
            Assert.True(symbol.HasTag("test2"));
        }

        [Fact]
        public void CanAddRemoveMultipleTags()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test1"));
            Assert.True(symbol.AddTag("test2"));
            Assert.True(symbol.RemoveTag("test2"));
            Assert.False(symbol.HasTag("test2"));
            Assert.True(symbol.RemoveTag("test1"));
            Assert.False(symbol.HasTag("test1"));
        }

        [Fact]
        public void CanAddRemoveMissingTags()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test1"));
            Assert.True(symbol.AddTag("test2"));
            Assert.False(symbol.RemoveTag("test3"));
        }

        [Fact]
        public void CanAdd2AndRemoveAllTags()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test1"));
            Assert.True(symbol.AddTag("test2"));
            Assert.True(symbol.RemoveTag("test1"));
            Assert.True(symbol.RemoveTag("test2"));
        }

        [Fact]
        public void CanAdd3AndRemoveAllTags()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test1"));
            Assert.True(symbol.AddTag("test2"));
            Assert.True(symbol.AddTag("test3"));
            Assert.True(symbol.RemoveTag("test1"));
            Assert.True(symbol.RemoveTag("test3"));
            Assert.True(symbol.RemoveTag("test2"));
        }

        [Fact]
        public void CanAdd3AndRemoveMissingTags()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.True(symbol.AddTag("test1"));
            Assert.True(symbol.AddTag("test2"));
            Assert.True(symbol.AddTag("test3"));
            Assert.False(symbol.RemoveTag("test4"));
            Assert.True(symbol.RemoveTag("test1"));
            Assert.True(symbol.RemoveTag("test3"));
            Assert.True(symbol.RemoveTag("test2"));
        }
    }
}
