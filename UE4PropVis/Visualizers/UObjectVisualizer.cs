// Copyright 2017-2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;

using UE4PropVis.Core;
using UE4PropVis.Core.EE;
using UE4PropVis.Resources;
using UE4PropVis.Constants;


namespace UE4PropVis
{
	class UObjectVisualizer : UE4Visualizer
	{
		public class Factory : IUE4VisualizerFactory
		{
			public UE4Visualizer CreateVisualizer(DkmVisualizedExpression expression)
			{
				return new UObjectVisualizer(expression);
			}
		}

		private enum EvaluationState
		{
			Uninitialized,
			MinimalEvaluation,		// Evaluated only as a condensed display string. Access context will not exist.
			Evaluated,				// Fully evaluated, though expansion may or may not have been processed.
			Failed,
		}

		private EvaluationState state_ = EvaluationState.Uninitialized;
		private DkmEvaluationResult eval_;
		private bool proplist_shown_ = false;
		private UPropertyAccessContext access_ctx_;

		public UObjectVisualizer(DkmVisualizedExpression expression) : base(expression)
		{
			// @NOTE: Moved here since EvaluationResult was always retrieved immediately after
			// successful construction.
			EvaluateExpressionResult();

			if (state_ == EvaluationState.Evaluated)
			{
				// Initialize the context object which we use to access properties on the object.
				// This can potentially be shared with a PropertyListVisualizer child.
				access_ctx_ = new UPropertyAccessContext(expression_);
			}
		}

		// @TODO: Feels like C# properties aren't intended to do so much work...?
		public override DkmEvaluationResult EvaluationResult
		{
			get
			{
				return eval_;
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
			// First, default expansion

			// @TODO: Calling this both in EvaluateExpressionResult, and from UseDefaultEvaluationBehavior
			// in the component. Be good if we can somehow test if this is actually redoing the evaluation,
			// or just returning the existing one.
			var eval = DefaultEE.DefaultEval(expression_, true);
			DkmEvaluationResult[] children;
			DkmEvaluationResultEnumContext default_enum_ctx;

            // @TODO: Work out why this fails sometimes. Seems to happen when the non-expanded preview yields Name=", Class=".
            try
            {
                expression_.GetChildrenCallback(eval, 0, expression_.InspectionContext, out children, out default_enum_ctx);
            }
            catch
            {
                enumContext = DkmEvaluationResultEnumContext.Create(
                    0,
                    expression_.StackFrame,
                    expression_.InspectionContext,
                    null
                );
                return;
            }

			// Now any custom additions to the expansion
			int custom_children = 0;

			// Need to determine if we should display the property list child
			DeterminePropertyListVisibility();
			if(IsPropertyListShown)
			{
				custom_children++;
			}

			// Create an aggregate enumeration context, with the length of the default enum, plus what we are adding on.
			// Store the default enumeration context inside it as a data item.
			enumContext = DkmEvaluationResultEnumContext.Create(
				default_enum_ctx.Count + custom_children,
				expression_.StackFrame,
				expression_.InspectionContext,
				new DefaultEnumContextDataItem(default_enum_ctx)
				);
		}

		public override void GetChildItems(DkmEvaluationResultEnumContext enumContext, int start, int count, out DkmChildVisualizedExpression[] items)
		{
			// Cap the requested number to the total remaining from startIndex
			count = Math.Min(count, enumContext.Count - start);
			items = new DkmChildVisualizedExpression[count];
			uint idx = 0;

			// Retrieve the default expansion enum context
			var default_data = enumContext.GetDataItem<DefaultEnumContextDataItem>();

			if (start < default_data.Context.Count)
			{
				// Requesting default children

				int default_count = Math.Min(count, default_data.Context.Count - start);
				DkmEvaluationResult[] default_evals;
				expression_.GetItemsCallback(default_data.Context, start, default_count, out default_evals);
				for (int dft_idx = 0; dft_idx < default_count; ++dft_idx, ++idx)
				{
					DkmSuccessEvaluationResult success_eval = default_evals[dft_idx] as DkmSuccessEvaluationResult;
					DkmExpressionValueHome home = null;
					if (success_eval != null && success_eval.Address != null)
					{
						home = DkmPointerValueHome.Create(success_eval.Address.Value);
					}
					else
					{
						home = DkmFakeValueHome.Create(0);
					}

					items[idx] = DkmChildVisualizedExpression.Create(
						enumContext.InspectionContext,
						expression_.VisualizerId,	// @TODO: Check this is what we want. Will we get callbacks for it, regardless of its type?
						expression_.SourceId,
						enumContext.StackFrame,
						home,
						default_evals[dft_idx],
						expression_,
						(uint)start,
						null
						);
				}
			}

			if (start + count > default_data.Context.Count)
			{
				// Requesting custom children
				// @NOTE: Currently just assuming only 1 custom child (prop list) and hard coding as such.

				// DkmSuccessEvaluationResult.ExtractFromProperty(IDebugProperty3!!!!!!!) ...............................................

				// @NOTE: Had thought could just create an expression with a null evaluation
				// inside it, and by giving it a visualizer guid, the system would call back
				// to us to evaluate the expression. Seems not to work though, I guess because the
				// visualizer guid identifies the visualizer but not the containing component,
				// and since the expression itself doesn't have a type, it can't know that it 
				// should call our component.
				DkmEvaluationResult eval = DkmSuccessEvaluationResult.Create(
					enumContext.InspectionContext,
					enumContext.StackFrame,
					Resources.UE4PropVis.IDS_DISP_BLUEPRINTPROPERTIES,
					Resources.UE4PropVis.IDS_DISP_BLUEPRINTPROPERTIES,
					DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable,
					"",	// @TODO: something like "[<count> properties]"
					null,
					"",	// Type column
					DkmEvaluationResultCategory.Other,
					DkmEvaluationResultAccessType.None,
					DkmEvaluationResultStorageType.None,
					DkmEvaluationResultTypeModifierFlags.None,
					null,
					null,
					null,
					null
					);

				// This child is just for organization and does not correspond to anything in memory.
				DkmExpressionValueHome valueHome = DkmFakeValueHome.Create(0);

				var prop_list_expr = DkmChildVisualizedExpression.Create(
					enumContext.InspectionContext,
					Guids.Visualizer.PropertyList,
					// Associate the expression with ourselves, since we created it
					Guids.Component.VisualizerComponent,
					enumContext.StackFrame,
					valueHome,
					eval,
					expression_,
					idx,
					null
					);

				// Create a visualizer for the property list, and attach it to the expression.
				var prop_list_visualizer = new PropertyListVisualizer(prop_list_expr, access_ctx_);
				prop_list_expr.SetDataItem(DkmDataCreationDisposition.CreateAlways, new ExpressionDataItem(prop_list_visualizer));

				items[idx] = prop_list_expr;
            }
		}

		private bool IsPropertyListShown
		{
			get
			{
				return proplist_shown_;
			}
		}

		protected bool IsPointer(DkmSuccessEvaluationResult eval)
		{
			// @TODO: Confirm reliable. Alternative is to check for '*' within Type property.
			return eval.Flags.HasFlag(DkmEvaluationResultFlags.Address);
		}

		// Assumes already confirmed that we are dealing with a pointer
		protected bool IsPointerNull(DkmSuccessEvaluationResult eval)
		{
			return eval.Address.Value == 0;
		}

		protected void EvaluateExpressionResult()
		{
			// We're going to customize the unexpanded display string, as well as the expanded
			// view (if requested later).

			// @TODO: Really don't understand why, but when we invoke the default eval below, and we get 
			// reentrant calls for child member UObjects, they are coming back as root expressions
			// with an empty FullName. This subsequently fails to evaluate if we pass it through to 
			// default eval again. Perhaps this is somehow related to breaking the potential infinite
			// recursion of visualizing children in order to visualize the parent, but don't follow how it
			// it supposed to be dealt with.
			if (expression_.TagValue == DkmVisualizedExpression.Tag.RootVisualizedExpression)
			{
				var as_root = expression_ as DkmRootVisualizedExpression;
				if (as_root.FullName.Length == 0)
				{
					string display_str = "{...}";
					eval_ = DkmSuccessEvaluationResult.Create(
						expression_.InspectionContext,
						expression_.StackFrame,
						as_root.Name,
						as_root.Name,
						DkmEvaluationResultFlags.ReadOnly,
						display_str,
						"",
#if !VS2013
						as_root.Type,
#else
						"Unknown",
#endif
						DkmEvaluationResultCategory.Other,
						DkmEvaluationResultAccessType.None,
						DkmEvaluationResultStorageType.None,
						DkmEvaluationResultTypeModifierFlags.None,
						null,
						null,
						null,
						null
						);
					state_ = EvaluationState.MinimalEvaluation;
					return;
				}
			}
			//

			string custom_display_str = Resources.UE4PropVis.IDS_DISP_CONDENSED;
			DkmSuccessEvaluationResult proto_eval = null;
			bool is_pointer;
			bool is_null;
			string address_str = "";

			// @NOTE: Initially here we were executing the full default evaluation of our expression.
			// Problem is that this will call back into us for all UObject children of the expression,
			// because it needs to generate a visualization for them in order to construct its {member vals...} display string.
			// And then, we just ignore that anyway and display our own...

			// The following attempts to avoid that by casting our expression to void* and then evaluating that.
			// If it fails, we assume we are non-pointer. If it succeeds, we can determine from the result whether we are null or not.
			
			// @WARNING: This may not be so safe, since we can't determine whether evaluation failed because we tried to cast a non-pointer
			// to void*, or because the passed in expression was not valid in the first place. Believe we should be okay, since we should
			// only be receiving expressions that have already been determined to be suitable for our custom visualizer.

			// Ideally though, we'd be able to get a raw expression evaluation, without any visualization taking place.
			// Seems there must be a way to do this, but looks like it requires using a different API...
			const bool UseVoidCastOptimization = true;

			string default_expr_str = Utility.GetExpressionFullName(expression_);
			if(UseVoidCastOptimization)
			{
				default_expr_str = ExpressionManipulator.FromExpression(default_expr_str).PtrCast(Cpp.Void).Expression;
			}

			DkmEvaluationResult eval = DefaultEE.DefaultEval(default_expr_str, expression_, true);

			if (!UseVoidCastOptimization)
			{
				if (eval.TagValue != DkmEvaluationResult.Tag.SuccessResult)
				{
					// Genuine failure to evaluate the passed in expression
					eval_ = eval;
					state_ = EvaluationState.Failed;
					return;
				}
				else
				{
					proto_eval = (DkmSuccessEvaluationResult)eval;
					custom_display_str = proto_eval.Value;

					is_pointer = IsPointer(proto_eval);
					is_null = is_pointer && IsPointerNull(proto_eval);
					// @TODO: need to extract address string
				}
			}
			else
			{
				DkmDataAddress address = null;
				if (eval.TagValue != DkmEvaluationResult.Tag.SuccessResult)
				{
					// Assume the failure just implies the expression was non-pointer (thereby assuming that it was itself valid!)
					
					// @TODO: Could actually fall back onto standard evaluation here, in order to determine for sure
					// that the original expression is valid. Failure wouldn't be common since most of the time we are
					// dealing with pointer expressions, so any potential optimization should still be gained.

					is_pointer = false;
					is_null = false;
				}
				else
				{
					var success = (DkmSuccessEvaluationResult)eval;
                    Debug.Assert(IsPointer(success));
					is_pointer = true;
					is_null = is_pointer && IsPointerNull(success);
					address = success.Address;
					address_str = success.Value;
				}

				proto_eval = DkmSuccessEvaluationResult.Create(
						expression_.InspectionContext,
						expression_.StackFrame,
						"",
						"",
						DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable,
						"",
						"",
#if !VS2013
						Utility.GetExpressionType(expression_),
#else
						is_pointer ? "UObject *" : "UObject",
#endif
						DkmEvaluationResultCategory.Other,
						DkmEvaluationResultAccessType.None,
						DkmEvaluationResultStorageType.None,
						DkmEvaluationResultTypeModifierFlags.None,
						address,
						null,
						null,
						null
						);
			}

			string obj_expression_str = Utility.GetExpressionFullName(expression_);
			var obj_em = ExpressionManipulator.FromExpression(obj_expression_str);

			// Store pointer flags on the expression
			expression_.SetDataItem(
				DkmDataCreationDisposition.CreateAlways,
				new UObjectDataItem(is_pointer, is_null)
				);

			if (!is_pointer)
			{
				// All subsequent manipulations of the expression assume it starts as a pointer to a 
				// UObject-derived class, so if our expression is to a dereferenced UObject, just 
				// prefix an 'address of' to the expression string.
				obj_em = obj_em.AddressOf();
			}

			// Generate the condensed display string.
			if (is_null)
			{
				if (Config.Instance.CustomNullObjectPreview)
				{
					custom_display_str = "<NULL> UObject";
				}
				else
				{
					var null_eval = DefaultEE.DefaultEval("(void*)0", expression_, true) as DkmSuccessEvaluationResult;
					custom_display_str = null_eval.Value + " <NULL>";
				}
			}
			else if (Config.Instance.DisplayUObjectPreview)
			{
				// Prefix the address, if this is a pointer expression
				string address_prefix_str = "";
				if(is_pointer)
				{
					address_prefix_str = address_str + " ";
				}

				// Specialized display for UClass?
				bool uclass_specialized = false;
				if (Config.Instance.DisplaySpecializedUClassPreview)
				{
					var uclass_em = obj_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjClass);
					var _uclass_fname_expr_str = uclass_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
					string _obj_uclass_name_str = UE4Utility.GetFNameAsString(_uclass_fname_expr_str, expression_);
					// @TODO: To simplify and for performance reasons, just hard coding known UClass variants
					if (_obj_uclass_name_str == "Class" ||
						_obj_uclass_name_str == "BlueprintGeneratedClass" ||
						_obj_uclass_name_str == "WidgetBlueprintGeneratedClass")
					{
						var fname_expr_str = obj_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
						string obj_name_str = UE4Utility.GetFNameAsString(fname_expr_str, expression_);

						var parent_uclass_fname_expr_str = obj_em.PtrCast(Typ.UStruct).PtrMember(Memb.SuperStruct).PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
						// This will return null if lookup failed for any reason.
						// We'll assume this meant no super class exists (ie. we're dealing with UClass itself)
						string parent_uclass_name_str = UE4Utility.GetFNameAsString(parent_uclass_fname_expr_str, expression_);
						if (parent_uclass_name_str == null)
						{
							parent_uclass_name_str = "None";
						}

						custom_display_str = String.Format(
							"{0}{{ClassName='{1}', Parent='{2}'}}",
							address_prefix_str,
							obj_name_str,
							parent_uclass_name_str
							);
						uclass_specialized = true;
					}
				}

				if (!uclass_specialized)
				{
					// For standard UObject condensed display string, show the object FName and its UClass.
					// @TODO: The evaluations required for this may be a performance issue, since they'll be done for all UObject children of any default visualized
					// aggregate type, even when it is not expanded (the default behaviour is to create a {...} display list of child member visualizations).
					var fname_expr_str = obj_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
					string obj_name_str = UE4Utility.GetFNameAsString(fname_expr_str, expression_);

					var uclass_fname_expr_str = obj_em.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjClass).PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjName).Expression;
					string obj_uclass_name_str = UE4Utility.GetFNameAsString(uclass_fname_expr_str, expression_);

					custom_display_str = String.Format(
						"{0}{{Name='{1}', Class='{2}'}}",
						address_prefix_str,
						obj_name_str,
						obj_uclass_name_str
						);
				}
			}

			eval_ = DkmSuccessEvaluationResult.Create(
				proto_eval.InspectionContext,
				proto_eval.StackFrame,
				// Override the eval name with the original expression name, since it will
				// have inherited the ",!" suffix.
				Utility.GetExpressionName(expression_),
				Utility.GetExpressionFullName(expression_),
				proto_eval.Flags,
				custom_display_str,//success_eval.Value,
				proto_eval.EditableValue,
				proto_eval.Type,
				proto_eval.Category,
				proto_eval.Access,
				proto_eval.StorageType,
				proto_eval.TypeModifierFlags,
				proto_eval.Address,
				proto_eval.CustomUIVisualizers,
				proto_eval.ExternalModules,
				null
				);
			state_ = EvaluationState.Evaluated;
		}

		private void DeterminePropertyListVisibility()
		{
			// First check if our expession evaluates to a null pointer, in which case we never show
			// properties regardless of policy.
			var uobj_data = expression_.GetDataItem<UObjectDataItem>();
			if(uobj_data.IsNull)
			{
				proplist_shown_ = false;
				return;
			}

			var policy = Config.Instance.PropertyListDisplayPolicy;
			switch(policy)
			{
				case Config.PropListDisplayPolicyType.AlwaysShow:
					proplist_shown_ = true;
					return;

				case Config.PropListDisplayPolicyType.OnlyForRelevantObjectTypes:
					proplist_shown_ = DetermineCanObjectHaveProperties();
					return;

				case Config.PropListDisplayPolicyType.OnlyIfHasVisibleProperties:
					proplist_shown_ = DetermineDoesObjectHaveProperties();
					return;
			}
		}

		private bool DetermineCanObjectHaveProperties()
		{
			return access_ctx_.DetermineObjectCanHaveProperties();
		}

		private bool DetermineDoesObjectHaveProperties()
		{
			return access_ctx_.DetermineObjectDoesHaveProperties();
		}
    }
}


