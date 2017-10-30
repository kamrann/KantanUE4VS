using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UE4PropVis.Constants
{
	public static class Typ
	{
		public const string UObjectBase = "UObjectBase";
		public const string UObject = "UObject";
		public const string UStruct = "UStruct";
		public const string UClass = "UClass";
		public const string UProperty = "UProperty";
		public const string Bool = "bool";
		public const string Byte = "uint8";
		public const string Int = "int32";
		public const string Float = "float";
		public const string String = "FString";
		public const string Name = "FName";
		public const string Text = "FText";
		public const string SoftObjectPtr = "FSoftObjectPtr";
		public const string WeakObj = "TWeakObjectPtr";
		public const string LazyObj = "TLazyObjectPtr";
		public const string Array = "TArray";
		public const string Map = "TMap";
		public const string DefaultAlloc = "FDefaultAllocator";
	}

	public static class Prop
	{
		public const string Bool = "BoolProperty";
		public const string Byte = "ByteProperty";
		public const string Int = "IntProperty";
		public const string Float = "FloatProperty";
		public const string String = "StrProperty";
		public const string Name = "NameProperty";
		public const string Text = "TextProperty";
		public const string ObjectBase = "ObjectPropertyBase";
		public const string Object = "ObjectProperty";
		public const string Class = "ClassProperty";
		public const string Struct = "StructProperty";
		public const string SoftObject = "SoftObjectProperty";
		public const string SoftClass = "SoftClassProperty";
		public const string WeakObj = "WeakObjectProperty";
		public const string LazyObj = "LazyObjectProperty";
		public const string Interface = "InterfaceProperty";
		public const string Array = "ArrayProperty";
		public const string Map = "MapProperty";
	}

	public static class CppProp
	{
		private const string Pfx = "U";

		public const string Bool = Pfx + Prop.Bool;
		public const string Byte = Pfx + Prop.Byte;
		public const string Int = Pfx + Prop.Int;
		public const string Float = Pfx + Prop.Float;
		public const string String = Pfx + Prop.String;
		public const string Name = Pfx + Prop.Name;
		public const string Text = Pfx + Prop.Text;
		public const string ObjectBase = Pfx + Prop.ObjectBase;
		public const string Object = Pfx + Prop.Object;
		public const string Class = Pfx + Prop.Class;
		public const string Struct = Pfx + Prop.Struct;
		public const string SoftObject = Pfx + Prop.SoftObject;
		public const string SoftClass = Pfx + Prop.SoftClass;
		public const string WeakObj = Pfx + Prop.WeakObj;
		public const string LazyObj = Pfx + Prop.LazyObj;
		public const string Interface = Pfx + Prop.Interface;
		public const string Array = Pfx + Prop.Array;
		public const string Map = Pfx + Prop.Map;
	}

	public static class Memb
	{
		// UObjectBase
		public const string ObjName = "NamePrivate";
		public const string ObjClass = "ClassPrivate";
		public const string ObjOuter = "OuterPrivate";
		public const string ObjFlags = "ObjectFlags";

		// UStruct
		public const string SuperStruct = "SuperStruct";
		public const string FirstProperty = "PropertyLink";

		// UClass
		public const string ClassFlags = "ClassFlags";

		// UProperty
		public const string PropOffset = "Offset_Internal";
		public const string NextProperty = "PropertyLinkNext";

		// UBoolProperty
		public const string ByteOffset = "ByteOffset";
		public const string FieldMask = "FieldMask";

		// UByteProperty
		public const string EnumType = "Enum";

		// UObjectPropertyBase
		public const string ObjectSubtype = "PropertyClass";

		// UClassProperty/USoftClassProperty
		public const string ClassSubtype = "MetaClass";

		// UStructProperty
		public const string CppStruct = "Struct";

		// UArrayProperty
		public const string ArrayInner = "Inner";

		// UEnum
		public const string EnumCppForm = "CppForm";

		// FName
		public const string CompIndex = "ComparisonIndex";
	}

	public static class ObjFlags
	{
		private const string Qual = "EObjectFlags::";

		public const string Native = Qual + "RF_MarkAsNative";
	}

	public static class ClassFlags
	{
		private const string Qual = "EClassFlags::";

		public const string Native = Qual + "CLASS_Native";
	}
}
