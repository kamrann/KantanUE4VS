
using System;
using System.Collections.Generic;

namespace KUE4VS
{
    public enum CodeElementType
    {
        Type,
        Source,
        Module,
        Plugin,
    };

    public enum SourceFileAdditionMode
    {
        HeaderAndCpp,
        HeaderOnly,
        CppOnly,
    };

    public enum AddableTypeVariant
    {
        UClass,
        UStruct,
        UInterface,
        UEnum,
        RawClass,
        RawStruct,
    };

    public enum PluginDefaultConfig
    {
        Runtime,
        Editor,
        RuntimeAndEditor,
        ContentOnly,
    };


    public static partial class Constants
    {
        public static readonly Dictionary<AddableTypeVariant, string> TypeKeywords
            = new Dictionary<AddableTypeVariant, string>
            {
                { AddableTypeVariant.UClass, "class" },
                { AddableTypeVariant.UStruct, "struct" },
                { AddableTypeVariant.UInterface, "class" },
                { AddableTypeVariant.UEnum, "enum class" },
                { AddableTypeVariant.RawClass, "class" },
                { AddableTypeVariant.RawStruct, "struct" },
            };

        public static readonly Dictionary<AddableTypeVariant, string> DefaultTypePrefixes
            = new Dictionary<AddableTypeVariant, string>
            {
                { AddableTypeVariant.UClass, "U" },
                { AddableTypeVariant.UStruct, "F" },
                { AddableTypeVariant.UInterface, "U" },
                { AddableTypeVariant.UEnum, "E" },
                { AddableTypeVariant.RawClass, "F" },
                { AddableTypeVariant.RawStruct, "F" },
            };

        public static readonly Dictionary<AddableTypeVariant, bool> ReflectedTypes
            = new Dictionary<AddableTypeVariant, bool>
            {
                { AddableTypeVariant.UClass, true },
                { AddableTypeVariant.UStruct, true },
                { AddableTypeVariant.UInterface, true },
                { AddableTypeVariant.UEnum, true },
                { AddableTypeVariant.RawClass, false },
                { AddableTypeVariant.RawStruct, false },
            };
    }

}
