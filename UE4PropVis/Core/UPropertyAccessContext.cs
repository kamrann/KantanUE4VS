using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger.Evaluation;

using UE4PropVis.Core.EE;
using UE4PropVis.Constants;


namespace UE4PropVis.Core
{
	class UPropertyAccessContext
	{
		// Used for evaluations
		private DkmVisualizedExpression context_expr_;

		// Base expression manipulator resolving to a pointer to the UObject
		private ExpressionManipulator obj_em_;

		// This stores our progress in evaluating properties.
		// It allows us to do whatever work is necessary at a given stage (for example, counting how many
		// properties we have) without doing work that can be put off. If/when we need to further the enumeration,
		// we just pick up from here.
		
		// @TODO: Implement this.
		// Will need to store a list of structures, containing data for each property that has been
		// enumerated. Probably want to have partial evaluation of individual properties as well as 
		// partial in the sense of only some properties having been processed.
		private ExpressionManipulator next_prop_em_;

		// NOTE: 'expr' must resolve to either <UObject-type>* or <UObject-type>.
		// Furthermore, it must have already been passed to a UObjectVisualizer, which has 
		// performed the initial evalution.
		public UPropertyAccessContext(DkmVisualizedExpression expr)
		{
			context_expr_ = expr;

			string base_expression_str = Utility.GetExpressionFullName(context_expr_);
			base_expression_str = Utility.StripExpressionFormatting(base_expression_str);

			obj_em_ = ExpressionManipulator.FromExpression(base_expression_str);

			// Determine if our base expression is <UObject-type>* or <UObject-type>
			var uobj_data = context_expr_.GetDataItem<UObjectDataItem>();
			Debug.Assert(uobj_data != null);
            if (!uobj_data.IsPointer)
			{
				obj_em_ = obj_em_.AddressOf();
			}
		}

		public bool DetermineObjectCanHaveProperties()
		{
			switch (Config.PropertyDisplayPolicy)
			{
				case Config.PropDisplayPolicyType.BlueprintOnly:
					{
						// See if the actual class of the object instance is native or not.
						var uclass_em = obj_em_.PtrCast(Typ.UObjectBase).PtrMember(Memb.ObjClass);
						var is_native_res = UE4Utility.TestUClassFlags(
							uclass_em.Expression,
							ClassFlags.Native,
							context_expr_
							);
						return is_native_res.IsValid && !is_native_res.Value;
					}

				case Config.PropDisplayPolicyType.All:
					return true;

				default:
					return false;
			}
		}

		public bool DetermineObjectDoesHaveProperties()
		{
			return false;
		}
    }
}

