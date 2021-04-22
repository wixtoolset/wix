// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class SymbolDefinitionFixture
    {
        [Fact]
        public void CanCreateFileSymbol()
        {
            var symbol = SymbolDefinitions.File.CreateSymbol();
            Assert.IsType<FileSymbol>(symbol);
            Assert.Same(SymbolDefinitions.File, symbol.Definition);
        }

        [Fact]
        public void CanCreateFileSymbolByName()
        {
            var symbol = SymbolDefinitions.ByName("File").CreateSymbol();
            Assert.IsType<FileSymbol>(symbol);
            Assert.Same(SymbolDefinitions.File, symbol.Definition);
        }

        //[Fact]
        //public void CanCreateFileSymbolByType()
        //{
        //    var symbol = SymbolDefinitions.CreateSymbol<FileSymbol>();
        //    Assert.Same(SymbolDefinitions.File, symbol.Definition);
        //}

        [Fact]
        public void CanSetComponentFieldInFileSymbolByCasting()
        {
            var fileSymbol = (FileSymbol)SymbolDefinitions.File.CreateSymbol();
            fileSymbol.ComponentRef = "Foo";
            Assert.Equal("Foo", fileSymbol.ComponentRef);
        }

        [Fact]
        public void CanCheckNameofField()
        {
            var fileSymbol = new FileSymbol();
            Assert.Equal("ComponentRef", fileSymbol.Definition.FieldDefinitions[0].Name);
            Assert.Null(fileSymbol.Fields[0]);
            fileSymbol.ComponentRef = "Foo";
            Assert.Equal("ComponentRef", fileSymbol.Fields[0].Name);
            Assert.Same(fileSymbol.Definition.FieldDefinitions[0].Name, fileSymbol.Fields[0].Name);
        }

        [Fact]
        public void CanSetComponentFieldInFileSymbolByNew()
        {
            var fileSymbol = new FileSymbol();
            fileSymbol.ComponentRef = "Foo";
            Assert.Equal("Foo", fileSymbol.ComponentRef);
        }

        [Fact]
        public void CanGetContext()
        {
            using (new IntermediateFieldContext("bar"))
            {
                var fileSymbol = new FileSymbol();
                fileSymbol.ComponentRef = "Foo";

                var field = fileSymbol[FileSymbolFields.ComponentRef];
                Assert.Equal("Foo", field.AsString());
                Assert.Equal("bar", field.Context);
            }
        }

        [Fact]
        public void CanSetInNestedContext()
        {
            var fileSymbol = new FileSymbol();

            using (new IntermediateFieldContext("bar"))
            {
                fileSymbol.ComponentRef = "Foo";

                var field = fileSymbol[FileSymbolFields.ComponentRef];
                Assert.Equal("Foo", field.AsString());
                Assert.Equal("bar", field.Context);

                using (new IntermediateFieldContext("baz"))
                {
                    fileSymbol.ComponentRef = "Foo2";

                    field = fileSymbol[FileSymbolFields.ComponentRef];
                    Assert.Equal("Foo2", field.AsString());
                    Assert.Equal("baz", field.Context);

                    Assert.Equal("Foo", (string)field.PreviousValue);
                    Assert.Equal("bar", field.PreviousValue.Context);
                }

                fileSymbol.ComponentRef = "Foo3";

                field = fileSymbol[FileSymbolFields.ComponentRef];
                Assert.Equal("Foo3", field.AsString());
                Assert.Equal("bar", field.Context);

                Assert.Equal("Foo2", (string)field.PreviousValue);
                Assert.Equal("baz", field.PreviousValue.Context);

                Assert.Equal("Foo", (string)field.PreviousValue.PreviousValue);
                Assert.Equal("bar", field.PreviousValue.PreviousValue.Context);
            }

            fileSymbol.ComponentRef = "Foo4";

            var fieldOutside = fileSymbol[FileSymbolFields.ComponentRef];
            Assert.Equal("Foo4", fieldOutside.AsString());
            Assert.Null(fieldOutside.Context);
        }

        //[Fact]
        //public void CanSetComponentFieldInFileSymbol()
        //{
        //    var fileSymbol = SymbolDefinitions.File.CreateSymbol<FileSymbol>();
        //    fileSymbol.Component_ = "Foo";
        //    Assert.Equal("Foo", fileSymbol.Component_);
        //}

        //[Fact]
        //public void CanThrowOnMismatchSymbolType()
        //{
        //    var e = Assert.Throws<InvalidCastException>(() => SymbolDefinitions.File.CreateSymbol<ComponentSymbol>());
        //    Assert.Equal("Requested wrong type WixToolset.Data.Symbols.ComponentSymbol, actual type WixToolset.Data.Symbols.FileSymbol", e.Message);
        //}
    }
}
