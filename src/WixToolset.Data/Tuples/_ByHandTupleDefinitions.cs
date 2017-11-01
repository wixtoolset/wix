// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if false
namespace WixToolset.Data.Tuples
{
    using System;

    //public enum TupleDefinitionType
    //{
    //    Component,
    //    File,
    //    MustBeFromAnExtension,
    //}

    public static partial class TupleDefinitionsOriginal
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out TupleDefinitionType type) || type == TupleDefinitionType.MustBeFromAnExtension)
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(TupleDefinitionType type)
        {
            switch (type)
            {
                //case TupleDefinitionType.Component:
                //    return TupleDefinitions.Component;

                //case TupleDefinitionType.File:
                //    return TupleDefinitions.File;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        //public static T CreateTuple<T>() where T : IntermediateTuple
        //{
        //    if (TypeToName.TryGetValue(typeof(T), out var name))
        //    {
        //        return ByName(name)?.CreateTuple<T>();
        //    }

        //    return null;
        //}
    }
}
#endif
