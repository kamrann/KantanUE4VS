using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger.Evaluation;

using UE4PropVis.Core;
using UE4PropVis.Core.EE;
using UE4PropVis.Constants;


namespace UE4PropVis
{
	class PropertyListVisualizer : UE4Visualizer
	{
/*		public class Factory : IUE4VisualizerFactory
		{
			public UE4Visualizer CreateVisualizer(DkmVisualizedExpression expression)
			{
				return new PropertyListVisualizer(expression);
			}
		}
*/
		// Static dictionary mapping from UE4 property type names, to corresponding C++ types.
		private static Dictionary<string, string> ue4_proptype_map_;
		// Dictionary of format specifiers to use with property types.
		private static Dictionary<string, string> ue4_propformat_map_;

		static PropertyListVisualizer()
		{
			// Build the property dictionaries

			// @TODO: CPF_UObjectWrapper may be of relevance to precisely what C++ classes we should be mapping properties to.
			ue4_proptype_map_ = new Dictionary<string, string>();
			//ue4_proptype_map_[Prop.Bool] = Typ.Bool;
			ue4_proptype_map_[Prop.Int] = Typ.Int;
			//ue4_proptype_map_[Prop.Byte] = Typ.Byte;
			ue4_proptype_map_[Prop.Float] = Typ.Float;
			ue4_proptype_map_[Prop.String] = Typ.String;
			ue4_proptype_map_[Prop.Name] = Typ.Name;
			ue4_proptype_map_[Prop.Text] = Typ.Text;
			ue4_proptype_map_[Prop.Object] = Typ.UObject + " *";
			ue4_proptype_map_[Prop.Class] = Typ.UClass + " *";
			ue4_proptype_map_[Prop.SoftObject] = Typ.SoftObjectPtr;
			ue4_proptype_map_[Prop.SoftClass] = Typ.SoftObjectPtr;

			ue4_propformat_map_ = new Dictionary<string, string>();
		}

		// Collection of property expression evaluations that will be built when expansion is requested on the property list expression.
		private Dictionary<string, DkmEvaluationResult> prop_evals_;

		// Access context (created by parent UObjectVisualizer)
		UPropertyAccessContext access_ctx_;

		public PropertyListVisualizer(DkmVisualizedExpression proplist_expr, UPropertyAccessContext access_ctx) : base(proplist_expr)
		{
			prop_evals_ = new Dictionary<string, DkmEvaluationResult>();

			access_ctx_ = access_ctx;
		}

		protected DkmChildVisualizedExpression PropListExpression
		{
			get
			{
				return expression_ as DkmChildVisualizedExpression;
			}
		}

		public override DkmEvaluationResult EvaluationResult
		{
			get
			{
				return PropListExpression.EvaluationResult;
            }
		}

		public override bool WantsCustomExpansion
		{
			get
			{
				return true;
			}
		}

		public override void PrepareExpansion(out DkmEvaluationResultEnumContext enumContext)
		{
			EvaluateProperties();

			enumContext = DkmEvaluationResultEnumContext.Create(
				GetNumProperties(),
				expression_.StackFrame,
				expression_.InspectionContext,
				null
				);
		}

		public override void GetChildItems(DkmEvaluationResultEnumContext enumContext, int start, int count, out DkmChildVisualizedExpression[] items)
		{
			var props = GetAllProperties();
			int total_num_props = props.Length;
			int end_idx = Math.Min(start + count, total_num_props);
			int num_to_return = end_idx - start;
			items = new DkmChildVisualizedExpression[num_to_return];

			for (int idx = start, out_idx = 0; idx < end_idx; ++idx, ++out_idx)
			{
				// We want to construct our own visualized expression from the eval.
				var eval = props[idx] as DkmSuccessEvaluationResult;
				// @TODO: Perhaps allow for failed evaluations and display anyway with unknown value??
				Debug.Assert(eval != null);

				DkmExpressionValueHome home;
				if (eval.Address != null)
				{
					home = DkmPointerValueHome.Create(eval.Address.Value);
				}
				else
				{
					home = DkmFakeValueHome.Create(0);
				}

				var expr = DkmChildVisualizedExpression.Create(
					expression_.InspectionContext,
					// @TODO: This is weird... seems to affect whether we get callback.
					// Obviously the properties can be of any type, UObject or not.
					// Perhaps best to put back the guid for PropertyValue, even though we don't really need to use it.
					Guids.Visualizer.PropertyValue,//Guids.Visualizer.UObject,//Guids.Visualizer.PropertyList,
					// Seems that in order for these to be passed back to the EE for default expansion, they need to be given
					// the SourceId from the originally received root expression.
					PropListExpression.Parent.SourceId,
					expression_.StackFrame,
					home,
					eval,
					expression_,
					(uint)idx,
					null	// Don't associate any data with the expression. If the EE calls back to us to expand it, we'll just tell it to use default expansion.
					);

				items[out_idx] = expr;
			}
		}

		public void EvaluateProperties()
		{
			List<DkmEvaluationResult> evals = new List<DkmEvaluationResult>();

			// Assume we've been constructed with the fabricated property list expression 
			DkmChildVisualizedExpression proplist_expr = (DkmChildVisualizedExpression)expression_;
			Debug.Assert(proplist_expr != null);

			// start could be an expression with the type of any UObject-derived class
			DkmVisualizedExpression start_expr = proplist_expr.Parent;

			string base_expression_str = Utility.GetExpressionFullName(start_expr);
			base_expression_str = Utility.StripExpressionFormatting(base_expression_str);

			ExpressionManipulator obj_em = null;
			// @TODO: Deal with non-pointer start expression
			obj_em = ExpressionManipulator.FromExpression(base_expression_str);

			// Determine if our base expression is <UObject-type>* or <UObject-type>
			bool is_pointer = start_expr.GetDataItem<UObjectDataItem>().IsPointer;
			if(!is_pointer)
			{
				obj_em = obj_em.AddressOf();
			}

			var uclass_em = obj_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjClass);

			if (Config.PropertyDisplayPolicy == Config.PropDisplayPolicyType.BlueprintOnly)
			{
				// See if the actual class of the object instance whose properties we want to enumerate
				// is native or not.
				var is_native_res = UE4Utility.TestUClassFlags(
					uclass_em.Expression,
					ClassFlags.Native,
					start_expr
					);

				// If the instance class is native, then it can't possibly have any non-native properties,
				// so bail out now.
				// @TODO: Even if the class is not native, we should still be able to avoid doing all the work
				// for enumerating every native property in order to find the non-native ones...
				// @TODO: How best to deal with failed is_native evaluation?
				if(is_native_res.IsValid && is_native_res.Value)
				{
					return;
				}
			}

			// Get the UStruct part of the UClass, in order to begin iterating properties
			var ustruct_em = uclass_em.PtrCast(Typ.UStruct);
			// Now access PropertyLink member, which is the start of the linked list of properties
			var prop_em = ustruct_em.PtrMember(Memb.FirstProperty);

			uint idx = 0;
			while (true)
			{
				Debug.Print("UE4PV: Invoking raw eval on UProperty* expression");

				var prop_eval = DefaultEE.DefaultEval(prop_em.Expression, start_expr, true) as DkmSuccessEvaluationResult;
				Debug.Assert(prop_eval != null);

				if (prop_eval.Address.Value == 0)
				{
					// nullptr, end of property list
					break;
				}

				bool should_skip = false;

				if (!should_skip && Config.PropertyDisplayPolicy == Config.PropDisplayPolicyType.BlueprintOnly)
				{
					// Check to see if this property is native or blueprint
					// We can test this by getting the UProperty's Outer, and checking its object flags for RF_Native.
					var prop_outer_em = prop_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjOuter);

					// @NOTE: RF_Native has gone, and checking for RF_MarkAsNative never seems to return true...
					//var is_native_res = UE4Utility.TestUObjectFlags(prop_outer_em.Expression, ObjFlags.Native, start_expr);

					// So, access class flags instead.
					// Note that we make the assumption here that the property's outer is a UClass, which should be safe since
					// we're starting out with a uobject, so all properties, including inherited ones, should be outered to a uclass.
					var prop_outer_uclass_em = prop_outer_em.PtrCast(Typ.UClass);
					var is_native_res = UE4Utility.TestUClassFlags(prop_outer_uclass_em.Expression, ClassFlags.Native, start_expr);

					// According to UE4 UStruct API docs, property linked list is ordered from most-derived
					// to base. If so, we should be able to bail out here knowing that having hit a native property,
					// all following properties must be native too.
					if (is_native_res.IsValid && is_native_res.Value)
					{
						return;
					}
				}

				if (!should_skip)
				{
					// @TODO: Here we use the starting expression for the container.
					// May not work if the root expression was not of pointer type!!
					var prop_val_eval = GeneratePropertyValueEval(
						obj_em.Expression,
						prop_em.Expression,
						idx,
						start_expr
						);
					if (prop_val_eval != null && !Config.IsPropertyHidden(prop_val_eval.Name))
					{
						prop_evals_[prop_val_eval.Name] = prop_val_eval;
						++idx;
					}
				}

				// Advance to next link
				prop_em = prop_em.PtrMember(Memb.NextProperty);
			}
		}

		public int GetNumProperties()
		{
			return prop_evals_.Count;
		}

		public DkmEvaluationResult[] GetAllProperties()
		{
			DkmEvaluationResult[] props = new DkmEvaluationResult[prop_evals_.Count];
			int idx = 0;
			foreach(var eval in prop_evals_)
			{
				props[idx] = eval.Value;
				++idx;
			}
			return props;
		}

		protected static string GetBoolPropertyByteOffset(string uboolprop_expr_str, DkmVisualizedExpression context_expr)
		{
			var boolprop_em = ExpressionManipulator.FromExpression(uboolprop_expr_str).PtrMember(Memb.ByteOffset);
			// uint8 property
			var eval = DefaultEE.DefaultEval(boolprop_em.Expression, context_expr, true) as DkmSuccessEvaluationResult;
			var val_str = eval.Value;
			return Utility.GetNumberFromUcharValueString(val_str);
        }

		protected static string GetBoolPropertyFieldMask(string uboolprop_expr_str, DkmVisualizedExpression context_expr)
		{
			var boolprop_em = ExpressionManipulator.FromExpression(uboolprop_expr_str).PtrMember(Memb.FieldMask);
			// uint8 property
			var eval = DefaultEE.DefaultEval(boolprop_em.Expression, context_expr, true) as DkmSuccessEvaluationResult;
			var val_str = eval.Value;
			return Utility.GetNumberFromUcharValueString(val_str);
		}

		// Returns the type of property identified by the expression string
		// (As in, "IntProperty", "ObjectProperty", etc)
		protected string GetPropertyType(string uprop_expr_str, DkmVisualizedExpression context_expr)
		{
			// We get this from the value of the Name member of the UProperty's UClass.
			var uprop_em = ExpressionManipulator.FromExpression(uprop_expr_str);
			string prop_class_name_expr_str = uprop_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjClass).PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
			return UE4Utility.GetFNameAsString(prop_class_name_expr_str, context_expr);
		}

		public class CppTypeInfo
		{
			private string type_;
			private string display_;
			private string format_;

			public CppTypeInfo(string type, string display = null, string format = null)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}

				type_ = type;
				display_ = display != null ? display : type;
				format_ = (format != null && format.Length > 0) ? ("," + format) : String.Empty;
			}

			public string Type
			{
				get
				{
					return type_;
				}
			}

			public string Display
			{
				get
				{
					return display_;
				}
			}

			public string Format
			{
				get
				{
					return format_;
				}
			}
		}

		// Takes in a UE4 property type string (eg. IntPropery, ObjectProperty, etc) along with a
		// expression string which evaluates to a UProperty*, and maps to the corresponding C++ type.
		protected CppTypeInfo[] GetCppTypeForPropertyType(string prop_type, string uprop_expr_str, DkmVisualizedExpression context_expr)
		{
			switch (prop_type)
			{
				case Prop.Bool:
					return new CppTypeInfo[] {
						new CppTypeInfo("bool")
					};

				case Prop.Struct:
					{
						// This is gonna be effort.
						// Get UStructProperty
						var ustructprop_em = ExpressionManipulator.FromExpression(uprop_expr_str).PtrCast(CppProp.Struct);
						// Need to access its UScriptStruct* member 'Struct', and get its object name.
						var struct_name_expr_str = ustructprop_em.PtrMember(Memb.CppStruct).PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
						// Get that name string
						string struct_name = UE4Utility.GetFNameAsString(struct_name_expr_str, context_expr);
						// Add the 'F' prefix
						return new CppTypeInfo[] {
							new CppTypeInfo("F" + struct_name)
						};
					}

				case Prop.Byte:
					{
						// Could be plain uint8, or a UEnum.
						// Get UByteProperty
						var ubyteprop_em = ExpressionManipulator.FromExpression(uprop_expr_str).PtrCast(CppProp.Byte);
						// Need to access its UEnum* member 'Enum'.
						var uenum_em = ubyteprop_em.PtrMember(Memb.EnumType);
						// Evaluate this to see if it is null or not
						bool is_enum_valid = !UE4Utility.IsPointerNull(uenum_em.Expression, context_expr);
						if (is_enum_valid)
						{
							// This property is an enum, so the type we want is the fully qualified C++ enum type.

							// @NOTE: Seems that the CppType member should be exactly what we need, but appears to actually be unused.
							//string cpp_enum_name_expr_str = uenum_em.PtrMember("CppType").Expression;

							string uenum_fname_expr_str = uenum_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
							string uenum_name = UE4Utility.GetFNameAsString(uenum_fname_expr_str, context_expr);

							// We need to know if it's a namespaced enum or not
							string is_namespaced_enum_expr_str = String.Format(
								"{0}==UEnum::ECppForm::Namespaced",
								uenum_em.PtrMember(Memb.EnumCppForm).Expression
								);
							var is_namespaced_res = UE4Utility.EvaluateBooleanExpression(is_namespaced_enum_expr_str, context_expr);
							// @TODO: on evaluation failure??
							CppTypeInfo primary;
							if (is_namespaced_res.IsValid && is_namespaced_res.Value)
							{
								// A namespaced enum type should (hopefully) always be <UEnum::Name>::Type
								primary = new CppTypeInfo(String.Format("{0}::Type", uenum_name), uenum_name);
							}
							else
							{
								// For regular or enum class enums, the full type name should be just the name of the UEnum object.
								primary = new CppTypeInfo(uenum_name);
							}

							return new CppTypeInfo[] {
								primary,
								// Fallback onto regular byte display, in case enum name symbol not available
								new CppTypeInfo(Typ.Byte, uenum_name + "?")
							};
						}
						else
						{
							// Must just be a regular uint8
							return new CppTypeInfo[] {
								new CppTypeInfo(Typ.Byte)
							};
						}
					}

				case Prop.Array:
					{
						// Okay, cast to UArrayProperty and get the inner property type
						var array_prop_em = ExpressionManipulator.FromExpression(uprop_expr_str).PtrCast(CppProp.Array);
						var inner_prop_em = array_prop_em.PtrMember(Memb.ArrayInner);

						// @TODO: Need to look into how TArray< bool > is handled.
						string inner_prop_type = GetPropertyType(inner_prop_em.Expression, context_expr);
						var inner_cpp_type_info = GetCppTypeForPropertyType(inner_prop_type, inner_prop_em.Expression, context_expr);

						var result = new CppTypeInfo[inner_cpp_type_info.Length];
						for (int i = 0; i < inner_cpp_type_info.Length; ++i)
						{
							// Type needed is TArray< %inner_cpp_type%, FDefaultAllocator >
							string type = String.Format("{0}<{1},{2}>", Typ.Array, inner_cpp_type_info[i].Type, Typ.DefaultAlloc);
							// Omit allocator from display string, since for UPROPERTY arrays it can't be anything else
							string display = String.Format("{0}<{1}>", Typ.Array, inner_cpp_type_info[i].Display);
							result[i] = new CppTypeInfo(type, display);
                        }
						return result;
					}

				case Prop.Object:
				case Prop.SoftObject:
					{
						if(!Config.ShowExactUObjectTypes)
						{
							break;
						}

						string obj_cpp_type_name = Typ.UObject;

						// Need to find out the subtype of the property, which is specified by UObjectPropertyBase::PropertyClass
						var objprop_em = ExpressionManipulator.FromExpression(uprop_expr_str).PtrCast(CppProp.ObjectBase);
						var subtype_uclass_em = objprop_em.PtrMember(Memb.ObjectSubtype);
						var uclass_fname_em = subtype_uclass_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName);
						string uclass_fname = UE4Utility.GetFNameAsString(uclass_fname_em.Expression, context_expr);

						// Is the property class native?
						var is_native_res = UE4Utility.IsNativeUClassOrUInterface(subtype_uclass_em.Expression, context_expr);
						// @TODO: currently not really handling failed eval
						bool is_native = is_native_res.IsValid ? is_native_res.Value : true;
						string native_uclass_fname;
						if (is_native)
						{
							// Yes
							native_uclass_fname = uclass_fname;
						}
						else
						{
							// No, we need to retrieve the name of its native base
							native_uclass_fname = UE4Utility.GetBlueprintClassNativeBaseName(subtype_uclass_em.Expression, context_expr);
						}

						Debug.Assert(native_uclass_fname != null);

						// Now we have to convert the unprefixed name, to a prefixed C++ type name
						obj_cpp_type_name = UE4Utility.DetermineNativeUClassCppTypeName(native_uclass_fname, context_expr);

						string uclass_display_name = UE4Utility.GetBlueprintClassDisplayName(uclass_fname);
						switch (prop_type)
						{
							case Prop.Object:
								{
									// if not native, add a suffix to the display type showing the blueprint class of the property
									// @NOTE: this is nothing to do with what object the value points to and what its type may be. property meta data only.
									string suffix = is_native ? String.Empty : String.Format(" [{0}]", uclass_display_name);
									string primary_type = String.Format("{0} *", obj_cpp_type_name);
									string primary_display = String.Format("{0} *{1}", obj_cpp_type_name, suffix);
									// fallback, no symbols available for the native base type, so use 'UObject' instead
									string fallback_type = String.Format("{0} *", Typ.UObject);
									string fallback_display = String.Format("{0}? *{1}", obj_cpp_type_name, suffix);

									return new CppTypeInfo[]
									{
										new CppTypeInfo(primary_type, primary_display),
										new CppTypeInfo(fallback_type, fallback_display)
									};
								}

							case Prop.SoftObject:
								{
									// @NOTE: Don't really see anything to gain by casting to TAssetPtr< xxx >, since it's just another level of encapsulation that isn't
									// needed for visualization purposes.
									string suffix = String.Format(" [{0}]", is_native ? obj_cpp_type_name : uclass_display_name);
									string primary_type = Typ.SoftObjectPtr; //String.Format("TAssetPtr<{0}>", obj_cpp_type_name);
									string primary_display = String.Format("{0}{1}", Typ.SoftObjectPtr, suffix);

									// If just using FAssetPtr, no need for a fallback since we don't need to evaluate the specialized template parameter type
									return new CppTypeInfo[]
									{
										new CppTypeInfo(primary_type, primary_display)
									};
								}

							default:
								Debug.Assert(false);
								return null;
						}
					}

/*				@TODO: Not so important. What's below is wrong, but essentially if we implement this, it's just to differentiate between UClass, UBlueprintGeneratedClass, etc
				case "ClassProperty":
				case "AssetClassProperty":
					{
						if (!Config.ShowExactUObjectTypes)
						{
							break;
						}

						// Need to find out the subtype of the property, which is specified by UClassProperty::MetaClass/UAssetClassProperty::MetaClass
						// Cast to whichever property type we are (either UClassProperty or UAssetClassProperty)
						string propclass_name = String.Format("U{0}", prop_type);
						var classprop_em = ExpressionManipulator.FromExpression(uprop_expr_str).PtrCast(propclass_name);
						// Either way, we have a member called 'MetaClass' which specified the base UClass stored in this property
						var subtype_uclass_em = classprop_em.PtrMember("MetaClass");
						var subtype_fname_em = subtype_uclass_em.PtrCast("UObjectBase").PtrMember("Name");
						string subtype_fname = UE4Utility.GetFNameAsString(subtype_fname_em.Expression, context_expr);
						return String.Format("U{0}*", subtype_fname);
					}
*/			}

			// Standard cases, just use cpp type stored in map.
			// If not found, null string will be returned.
			string cpp_type = null;
			if (ue4_proptype_map_.TryGetValue(prop_type, out cpp_type))
			{
				return new CppTypeInfo[] { new CppTypeInfo(cpp_type) };
			}
			else
			{
				return null;
			}
		}

		// Takes in an address string which identifies the location of the property value in memory,
		// along with property type information.
		// Outputs a string expression which will evaluate to the precise location with the correct C++ type.
		protected string AdjustPropertyExpressionStringForType(string address_str, string prop_type, string uprop_expr_str, DkmVisualizedExpression context_expr, CppTypeInfo cpp_type_info)
		{
			// Special cases first
			switch (prop_type)
			{
				case Prop.Bool:
					{
						// Needs special treatment since can be a single bit field as well as just a regular bool
						// Get a UBoolProperty context
						var uboolprop_em = ExpressionManipulator.FromExpression(uprop_expr_str).PtrCast(CppProp.Bool);
						// Read the byte offset and field mask properties
						string byte_offset_str = GetBoolPropertyByteOffset(uboolprop_em.Expression, context_expr);
						string field_mask_str = GetBoolPropertyFieldMask(uboolprop_em.Expression, context_expr);
						// Format an expression which adds the byte offset onto the address, derefs
						// and then bitwise ANDs with the field mask.
						return String.Format("(*({0} + {1}) & {2}) != 0",
							address_str,
							byte_offset_str,
							field_mask_str
							);
					}

				case Prop.Byte:
					{
						// Enum properties are a bit awkward, since due to the different ways the enums can be declared,
						// we can't reliably access them by a cast and dereference.
						// eg. A regular enum type will be considered 64 bits.
						// So, we need to use uint8 to get at the memory, and then do the cast, or rather conversion,
						// *after* dereferencing in order to get the correct type.
						return String.Format("({0})*({1}*){2}{3}",
							cpp_type_info.Type,
							Typ.Byte,
							address_str,
							cpp_type_info.Format
							);
					}

				default:
					break;
			}

			// If we got here, we just need to get the corresponding C++ type, then cast the address
			// to it and dereference.
			// [*(type*)address,fmt]
			return String.Format("*({0}*){1}{2}", cpp_type_info.Type, address_str, cpp_type_info.Format);
		}

		protected DkmEvaluationResult GeneratePropertyValueEval(string container_expr_str, string uprop_expr_str, uint index, DkmVisualizedExpression context_expr)
		{
			Debug.Print("UE4PV: Trying to generate property value for property #{0}", index + 1);

			var uprop_em = ExpressionManipulator.FromExpression(uprop_expr_str);

			// Get name of property
			string prop_name_expr_str = uprop_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
			string prop_name = UE4Utility.GetFNameAsString(prop_name_expr_str, context_expr);

			// And the property type
			string prop_type = GetPropertyType(uprop_em.Expression, context_expr);

			// Now, determine address of the actual property value
			// First requires container address
			// Cast to void* first, so that expression evaluation is simplified and faster
			container_expr_str = ExpressionManipulator.FromExpression(container_expr_str).PtrCast(Cpp.Void).Expression;
			var container_eval = DefaultEE.DefaultEval(container_expr_str, context_expr, true) as DkmSuccessEvaluationResult;
			Debug.Assert(container_eval != null && container_eval.Address != null);
			string address_str = container_eval.Address.Value.ToString();

			// Next need offset in container (which is an int32 property of the UProperty class)
			var offset_expr_str = uprop_em.PtrMember(Memb.PropOffset).Expression;
			var offset_eval = DefaultEE.DefaultEval(offset_expr_str, context_expr, true) as DkmSuccessEvaluationResult;
			string offset_str = offset_eval.Value;

			// Then need to create an expression which adds on the offset
			address_str = String.Format("((uint8*){0} + {1})", address_str, offset_str);

			// Next, we need to cast this expression depending on the type of property we have.
			// Retrieve a list of possible cast expressions.
			var cpp_type_info_list = GetCppTypeForPropertyType(prop_type, uprop_em.Expression, context_expr);

            if (cpp_type_info_list == null)
            {
                // Was not able to find an evaluatable expression.
                return null;
            }

            // Accept the first one that is successfully evaluated
            DkmSuccessEvaluationResult success_eval = null;
			string display_type = null;
            foreach (var cpp_type_info in cpp_type_info_list)
			{
				string prop_value_expr_str = AdjustPropertyExpressionStringForType(address_str, prop_type, uprop_em.Expression, context_expr, cpp_type_info);

				Debug.Print("UE4PV: Attempting exp eval as: '{0}'", prop_value_expr_str);

				// Attempt to evaluate the expression
				DkmEvaluationResult raw_eval = DefaultEE.DefaultEval(prop_value_expr_str, context_expr, false);
				if (raw_eval.TagValue == DkmEvaluationResult.Tag.SuccessResult)
				{
					// Success
					success_eval = raw_eval as DkmSuccessEvaluationResult;
					display_type = cpp_type_info.Display;
					break;
				}
			}

			if(success_eval == null)
			{
				// Was not able to find an evaluatable expression.
				return null;
			}

			return DkmSuccessEvaluationResult.Create(
				expression_.InspectionContext,
				expression_.StackFrame,
				prop_name,
				success_eval.FullName,//prop_value_expr_str,
				success_eval.Flags,
				success_eval.Value,
				// @TODO: Perhaps need to disable for some stuff, like bitfield bool?
				success_eval.EditableValue,
				display_type,//success_eval.Type,
				success_eval.Category,
				success_eval.Access,
				success_eval.StorageType,
				success_eval.TypeModifierFlags,
				success_eval.Address,
				success_eval.CustomUIVisualizers,
				success_eval.ExternalModules,
				null
				);
		}
	}
}
