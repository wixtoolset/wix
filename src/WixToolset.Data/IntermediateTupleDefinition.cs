// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public class IntermediateTupleDefinition
    {
        public IntermediateTupleDefinition(string name, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
            : this(TupleDefinitionType.MustBeFromAnExtension, name, fieldDefinitions, strongTupleType)
        {
        }

        internal IntermediateTupleDefinition(TupleDefinitionType type, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
            : this(type, type.ToString(), fieldDefinitions, strongTupleType)
        {
        }

        private IntermediateTupleDefinition(TupleDefinitionType type, string name, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
        {
            this.Type = type;
            this.Name = name;
            this.FieldDefinitions = fieldDefinitions;
            this.StrongTupleType = strongTupleType ?? typeof(IntermediateTuple);
#if DEBUG
            if (!this.StrongTupleType.IsSubclassOf(typeof(IntermediateTuple))) throw new ArgumentException(nameof(strongTupleType));
#endif
        }

        public TupleDefinitionType Type { get; }

        public string Name { get; }

        public IntermediateFieldDefinition[] FieldDefinitions { get; }

        private Type StrongTupleType { get; }

        public IntermediateTuple CreateTuple(SourceLineNumber sourceLineNumber = null, Identifier id = null)
        {
            var result = (this.StrongTupleType == typeof(IntermediateTuple)) ? (IntermediateTuple)Activator.CreateInstance(this.StrongTupleType, this) : (IntermediateTuple)Activator.CreateInstance(this.StrongTupleType);
            result.SourceLineNumbers = sourceLineNumber;
            result.Id = id;

            return result;
        }
    }
}
