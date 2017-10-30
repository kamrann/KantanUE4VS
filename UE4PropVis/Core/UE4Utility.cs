using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger.Evaluation;

using UE4PropVis.Core.EE;
using UE4PropVis.Constants;


namespace UE4PropVis
{
	public static class UE4Utility
	{
		// @NOTE: Currently ignoring FName::Number, and assuming valid.
		public static string GetFNameAsString(string expr_str, DkmVisualizedExpression context_expr)
		{
			var em = ExpressionManipulator.FromExpression(expr_str);
			em = em.DirectMember(Memb.CompIndex);
			DkmSuccessEvaluationResult comp_idx_eval = DefaultEE.DefaultEval(em.Expression, context_expr, true) as DkmSuccessEvaluationResult;

			// @TODO: For now, to avoid requiring more lookups, we'll allow a failure and return null,
			// and let the caller decide how to interpret it.
			//Debug.Assert(comp_idx_eval != null);
			if(comp_idx_eval == null)
			{
				return null;
			}

			string comp_idx_str = comp_idx_eval.Value;
			string ansi_expr_str = String.Format("((FNameEntry*)(((FNameEntry***)GFNameTableForDebuggerVisualizers_MT)[{0} / 16384][{0} % 16384]))->AnsiName", comp_idx_str);
			var ansi_eval = DefaultEE.DefaultEval(ansi_expr_str, context_expr, true);
			return ansi_eval.GetUnderlyingString();
		}

		// @TODO: Unsafe, assumes valid and non-empty.
		public static string GetFStringAsString(string expr_str, DkmVisualizedExpression context_expr)
		{
			var em = ExpressionManipulator.FromExpression(expr_str);
			em = em.DirectMember("Data").DirectMember("AllocatorInstance").DirectMember("Data").PtrCast("wchar_t");
            var data_eval = DefaultEE.DefaultEval(em.Expression, context_expr, true);

			Debug.Assert(data_eval != null);
			return data_eval.GetUnderlyingString();
		}

		// Given an expression which resolves to a pointer of any kind, returns the pointer value as an integer.
		public static ulong EvaluatePointerAddress(string pointer_expr_str, DkmVisualizedExpression context_expr)
		{
			ExpressionManipulator em = ExpressionManipulator.FromExpression(pointer_expr_str);
			// Cast to void* to avoid unnecessary visualization processing.
			string address_expr_str = em.PtrCast(Cpp.Void).Expression;
			var address_eval = (DkmEvaluationResult)DefaultEE.DefaultEval(address_expr_str, context_expr, true);
			if(address_eval.TagValue == DkmEvaluationResult.Tag.SuccessResult)
			{
				var eval = address_eval as DkmSuccessEvaluationResult;
                return eval.Address != null ? eval.Address.Value : 0;
			}
			return 0;
		}

		// Given an expression string that resolves to any kind of pointer, determines if the pointer is null.
		public static bool IsPointerNull(string pointer_expr_str, DkmVisualizedExpression context_expr)
		{
			return EvaluatePointerAddress(pointer_expr_str, context_expr) == 0;
		}

		// @TODO: Differentiate between false result and evaluation failure
		public static BoolEvaluation EvaluateBooleanExpression(string bool_expr_str, DkmVisualizedExpression context_expr)
		{
			try
			{
				var eval = DefaultEE.DefaultEval(bool_expr_str, context_expr, true);
				if (eval.TagValue == DkmEvaluationResult.Tag.SuccessResult)
				{
					var eval_result = eval as DkmSuccessEvaluationResult;
					return new BoolEvaluation(bool.Parse(eval_result.Value));
				}
				else
				{
					return BoolEvaluation.Indeterminate;
				}
			}
			catch(Exception)
			{
				return BoolEvaluation.Indeterminate;
			}
		}

		// Flags should be in a form that can be inserted into a bit test expression in the debuggee context
		public static BoolEvaluation TestExpressionFlags(string flags_var_expr_str, string flags, DkmVisualizedExpression context_expr)
		{
			string flag_test_expr_str = String.Format(
				"({0} & {1}) != 0",
				flags_var_expr_str,
				flags
				);
			return EvaluateBooleanExpression(flag_test_expr_str, context_expr);
		}

		// Flags should be in a form that can be inserted into a bit test expression in the 
		// debuggee context (eg. a raw integer, or a combination of RF_*** flags)
		public static BoolEvaluation TestUObjectFlags(string uobj_expr_str, string flags, DkmVisualizedExpression context_expr)
		{
			ExpressionManipulator em = ExpressionManipulator.FromExpression(uobj_expr_str);
			em = em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjFlags);
			var obj_flags_expr_str = em.Expression;

			// @TODO: Could do the flag test on this side by hard coding in the value of RF_Native, but for now
			// doing it the more robust way, and evaluating the expression in the context of the debugee.
			return TestExpressionFlags(obj_flags_expr_str, flags, context_expr);
		}

		public static BoolEvaluation TestUClassFlags(string uclass_expr_str, string flags, DkmVisualizedExpression context_expr)
		{
			ExpressionManipulator em = ExpressionManipulator.FromExpression(uclass_expr_str);
			em = em.PtrCast(Typ.UClass).PtrMember(Memb.ClassFlags);
			var class_flags_expr_str = em.Expression;

			// @TODO: Could do the flag test on this side by hard coding in the value of RF_Native, but for now
			// doing it the more robust way, and evaluating the expression in the context of the debugee.
			return TestExpressionFlags(class_flags_expr_str, flags, context_expr);
		}

		public static BoolEvaluation IsNativeUClassOrUInterface(string uclass_expr_str, DkmVisualizedExpression context_expr)
		{
			// @TODO: Think we should really check class flags for CLASS_Native, to potentially support interface classes too.
			// (see comment on flag in UE4 header)
			//return UE4Utility.TestUObjectFlags(uclass_expr_str, ObjFlags.Native, context_expr);

			return UE4Utility.TestUClassFlags(uclass_expr_str, ClassFlags.Native, context_expr);
		}

		// Given an expression string resolving to a UClass* (which is assumed to point to a **Blueprint** class), this determines
		// the UE4 (non-prefixed) name of the native base class.
		public static string GetBlueprintClassNativeBaseName(string uclass_expr_str, DkmVisualizedExpression context_expr)
		{
			var uclass_em = ExpressionManipulator.FromExpression(uclass_expr_str);
			do
			{
				var super_em = uclass_em.PtrCast(Typ.UStruct).PtrMember(Memb.SuperStruct).PtrCast(Typ.UClass);
				var is_native_res = IsNativeUClassOrUInterface(super_em.Expression, context_expr);
				if (!is_native_res.IsValid)
				{
					return null;
				}

				if(is_native_res.Value)
				{
					var name_em = super_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName);
					return GetFNameAsString(name_em.Expression, context_expr);
                }


				uclass_em = super_em;
			}
			while (DefaultEE.DefaultEval(uclass_em.PtrCast(Cpp.Void).Expression, context_expr, true).TagValue == DkmEvaluationResult.Tag.SuccessResult);

			// This shouldn't really happen
			return null;
		}

		// This just removes the '_C' suffix from the given blueprint class name
		public static string GetBlueprintClassDisplayName(string raw_uclass_fname)
		{
			if(raw_uclass_fname.IndexOf("_C") == raw_uclass_fname.Length - 2)
			{
				return raw_uclass_fname.Substring(0, raw_uclass_fname.Length - 2);
			}
			else
			{
				return String.Copy(raw_uclass_fname);
			}
		}

		// Determines the corresponding C++ name for the passed in UClass unprefixed name.
		public static string DetermineNativeUClassCppTypeName(string uclass_fname, DkmVisualizedExpression context_expr)
		{
			// Now we need to prefix the name, with either 'A' for actor-derived classes, or 'U' for all others.
			string cpp_prefix = "U";
			// This is kind of awkward. Cleanest approach would be to walk up the super-class chain, but that could be
			// a LOT of expression evaluations.
			// The following approach is hacky but should be quicker. Relying on the assumption that UE4 wouldn't allow 
			// both a UObject class and an AActor class with the same name (@TODO: is this true??), we just assume the 'U'
			// prefix, form an arbitrary expression using it, and see if it can be successfully evaluated by the debugger.
			// If not, we assume the prefix is 'A' instead.
			
			// @NOTE: This will fail if someone is stupid enough to create a reflected class 'AMyClass' as well
			// as a non-reflected class 'UMyClass'.

			// First do quick checks for common types, to avoid an unnecessary expression evaluation.
			if (uclass_fname == "Actor")
			{
				cpp_prefix = "A";
			}
			else if (uclass_fname != "Object")
			{
				// Assume 'U'.
				// Also, after casting to our assumed type, cast again to void*. This shouldn't affect the success/failure result
				// of the evaluation, but will avoid unnecessary visualization callbacks when the evaluation succeeds.
				string assumed_cpp_type = cpp_prefix + uclass_fname;
				string cast_expr_str = String.Format("(void*)({0}*)nullptr", assumed_cpp_type);
				var cast_eval = DefaultEE.DefaultEval(cast_expr_str, context_expr, true);
				if (cast_eval.TagValue != DkmEvaluationResult.Tag.SuccessResult)
				{
					// Cast failed, assume therefore must be an actor type
					cpp_prefix = "A";
				}
			}

			return cpp_prefix + uclass_fname;
		}
	}
}
