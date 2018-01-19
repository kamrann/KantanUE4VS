// Copyright 2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace KUE4VS
{
    public enum CodeElementType
    {
        [Display(Name = "New Type", Description = "Add code for a new UE4 reflected or native type.")]
        Type,
        [Display(Name = "Source File(s)", Description = "Add bare source files.")]
        Source,
        [Display(Name = "Module", Description = "Add a UE4 code module.")]
        Module,
        [Display(Name = "Plugin", Description = "Add a plugin.")]
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
        // @TODO: UEnum,
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
                //{ AddableTypeVariant.UEnum, "enum class" },
                { AddableTypeVariant.RawClass, "class" },
                { AddableTypeVariant.RawStruct, "struct" },
            };

        public static readonly Dictionary<AddableTypeVariant, string> DefaultTypePrefixes
            = new Dictionary<AddableTypeVariant, string>
            {
                { AddableTypeVariant.UClass, "U" },
                { AddableTypeVariant.UStruct, "F" },
                { AddableTypeVariant.UInterface, "U" },
                //{ AddableTypeVariant.UEnum, "E" },
                { AddableTypeVariant.RawClass, "F" },
                { AddableTypeVariant.RawStruct, "F" },
            };

        public static readonly Dictionary<AddableTypeVariant, bool> ReflectedTypes
            = new Dictionary<AddableTypeVariant, bool>
            {
                { AddableTypeVariant.UClass, true },
                { AddableTypeVariant.UStruct, true },
                { AddableTypeVariant.UInterface, true },
                //{ AddableTypeVariant.UEnum, true },
                { AddableTypeVariant.RawClass, false },
                { AddableTypeVariant.RawStruct, false },
            };
    }

}
