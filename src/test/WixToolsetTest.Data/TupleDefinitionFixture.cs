// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class TupleDefinitionFixture
    {
        [Fact]
        public void CanCreateFileTuple()
        {
            var tuple = TupleDefinitions.File.CreateTuple();
            Assert.IsType<FileTuple>(tuple);
            Assert.Same(TupleDefinitions.File, tuple.Definition);
        }

        [Fact]
        public void CanCreateFileTupleByName()
        {
            var tuple = TupleDefinitions.ByName("File").CreateTuple();
            Assert.IsType<FileTuple>(tuple);
            Assert.Same(TupleDefinitions.File, tuple.Definition);
        }

        //[Fact]
        //public void CanCreateFileTupleByType()
        //{
        //    var tuple = TupleDefinitions.CreateTuple<FileTuple>();
        //    Assert.Same(TupleDefinitions.File, tuple.Definition);
        //}

        [Fact]
        public void CanSetComponentFieldInFileTupleByCasting()
        {
            var fileTuple = (FileTuple)TupleDefinitions.File.CreateTuple();
            fileTuple.ComponentRef = "Foo";
            Assert.Equal("Foo", fileTuple.ComponentRef);
        }

        [Fact]
        public void CanCheckNameofField()
        {
            var fileTuple = new FileTuple();
            Assert.Equal("Component_", fileTuple.Definition.FieldDefinitions[0].Name);
            Assert.Null(fileTuple.Fields[0]);
            fileTuple.ComponentRef = "Foo";
            Assert.Equal("Component_", fileTuple.Fields[0].Name);
            Assert.Same(fileTuple.Definition.FieldDefinitions[0].Name, fileTuple.Fields[0].Name);
        }

        [Fact]
        public void CanSetComponentFieldInFileTupleByNew()
        {
            var fileTuple = new FileTuple();
            fileTuple.ComponentRef = "Foo";
            Assert.Equal("Foo", fileTuple.ComponentRef);
        }

        [Fact]
        public void CanGetContext()
        {
            using (new IntermediateFieldContext("bar"))
            {
                var fileTuple = new FileTuple();
                fileTuple.ComponentRef = "Foo";

                var field = fileTuple[FileTupleFields.ComponentRef];
                Assert.Equal("Foo", field.AsString());
                Assert.Equal("bar", field.Context);
            }
        }

        [Fact]
        public void CanSetInNestedContext()
        {
            var fileTuple = new FileTuple();

            using (new IntermediateFieldContext("bar"))
            {
                fileTuple.ComponentRef = "Foo";

                var field = fileTuple[FileTupleFields.ComponentRef];
                Assert.Equal("Foo", field.AsString());
                Assert.Equal("bar", field.Context);

                using (new IntermediateFieldContext("baz"))
                {
                    fileTuple.ComponentRef = "Foo2";

                    field = fileTuple[FileTupleFields.ComponentRef];
                    Assert.Equal("Foo2", field.AsString());
                    Assert.Equal("baz", field.Context);

                    Assert.Equal("Foo", (string)field.PreviousValue);
                    Assert.Equal("bar", field.PreviousValue.Context);
                }

                fileTuple.ComponentRef = "Foo3";

                field = fileTuple[FileTupleFields.ComponentRef];
                Assert.Equal("Foo3", field.AsString());
                Assert.Equal("bar", field.Context);

                Assert.Equal("Foo2", (string)field.PreviousValue);
                Assert.Equal("baz", field.PreviousValue.Context);

                Assert.Equal("Foo", (string)field.PreviousValue.PreviousValue);
                Assert.Equal("bar", field.PreviousValue.PreviousValue.Context);
            }

            fileTuple.ComponentRef = "Foo4";

            var fieldOutside = fileTuple[FileTupleFields.ComponentRef];
            Assert.Equal("Foo4", fieldOutside.AsString());
            Assert.Null(fieldOutside.Context);
        }

        //[Fact]
        //public void CanSetComponentFieldInFileTuple()
        //{
        //    var fileTuple = TupleDefinitions.File.CreateTuple<FileTuple>();
        //    fileTuple.Component_ = "Foo";
        //    Assert.Equal("Foo", fileTuple.Component_);
        //}

        //[Fact]
        //public void CanThrowOnMismatchTupleType()
        //{
        //    var e = Assert.Throws<InvalidCastException>(() => TupleDefinitions.File.CreateTuple<ComponentTuple>());
        //    Assert.Equal("Requested wrong type WixToolset.Data.Tuples.ComponentTuple, actual type WixToolset.Data.Tuples.FileTuple", e.Message);
        //}
    }
}
