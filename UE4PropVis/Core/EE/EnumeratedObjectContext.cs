using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;


namespace UE4PropVis.Core.EE
{
	/* @NOTE:
	This class  provides funcionality to access expression evaluations for class members by name.

	!!! Implementation is really a workaround !!!
	It calls back to the default EE to enumerate all children of the base expression, storing
	results in a map for access by name later. Suspect there is some way to directly access
	relative property data (perhaps somehow can get a IDebugProperty3 interface?) which would
	avoid doing a full enumeration.
	*/
	class EnumeratedObjectContext : ObjectContext
	{
		protected DkmSuccessEvaluationResult eval_;
		// @TODO: Rename.
		// These are not really members, but child expression properties.
		// They include things like 'most derived' and base classes.
		protected Dictionary<string, DkmChildVisualizedExpression> MemberExpressions;

		public class MyFactory : ObjectContext.Factory
		{
			public ObjectContext CreateObjectContext(DkmVisualizedExpression expr, DkmVisualizedExpression callback_expr)
			{
				return new EnumeratedObjectContext(expr, callback_expr);
			}
        };

		public EnumeratedObjectContext(DkmVisualizedExpression expr, DkmVisualizedExpression callback_expr) : base(expr, callback_expr)
		{
			EvaluateChildren();
		}

		public override ObjectContext.Factory GetFactory
		{
			get
			{
				return new MyFactory();
			}
		}

		public override DkmSuccessEvaluationResult GetEvaluationResult()
		{
			return eval_;
		}

		public override string GetClassName()
		{
			// May be prefixed with module tag ("<something or other>!<namespaced class name>").
			// If so, we want to return only the namespaced class name.
			string type = eval_.Type;
			int exclam = type.IndexOf('!');
			if (exclam == -1)
			{
				return type;
			}
			else
			{
				return type.Substring(exclam + 1);
			}
		}

		public override DkmChildVisualizedExpression GetMember(string name)
		{
			DkmChildVisualizedExpression result = null;
			MemberExpressions.TryGetValue(name, out result);
			return result;
		}

		public override DkmChildVisualizedExpression GetBaseClass(string class_name)
		{
			foreach (var child in MemberExpressions)
			{
				var eval = child.Value.EvaluationResult as DkmSuccessEvaluationResult;
				if(eval != null &&
					eval.Category == DkmEvaluationResultCategory.BaseClass &&
					// if class_name is null, just return the first base
					(class_name == null || child.Key == class_name))
				{
					return child.Value;
				}
			}

			return null;
		}

		public override DkmChildVisualizedExpression GetAncestorClass(string class_name)
		{
			// @TODO: What to do if the input expression is already the target type?
			ObjectContext Ctx = this;
			while(true)
			{
				var Base = Ctx.GetBaseClassContext(null);
				if(Base == null)
				{
					return null;
				}
				else if (Base.GetClassName() == class_name)
				{
					return (DkmChildVisualizedExpression)Base.Expression;
				}

				Ctx = Base;
			}
		}

/*		public override DkmChildVisualizedExpression GetMostDerived()
		{
			foreach (var child in MemberExpressions)
			{
				var eval = child.Value.EvaluationResult as DkmSuccessEvaluationResult;
				if (eval != null && eval.Category == DkmEvaluationResultCategory.MostDerivedClass)
				{
					return child.Value;
				}
			}

			return null;
		}
*/
		private void EvaluateChildren()
		{
			MemberExpressions = new Dictionary<string, DkmChildVisualizedExpression>();

			// @TODO: Am assuming that if expr_ is a child expression, it would be more
			// efficient to use its EvaluationResult property instead of invoking the default
			// evaluator again. However, doing this results in using child UObject expressions which
			// have been generated using the custom visualization (since ! specifier is not recursive).
			// We really don't want this since we just want the default expansion so we can navigate
			// through the members and bases of the class.
			// Problem is, don't know how to communicate to the 'UseDefaultEvaluationBehavior'
			// implementation to use a default expansion in this particular case. Setting the data
			// item on expr_ before calling GetItemsCallback doesn't work, since the expression that
			// gets passed through is not actually expr_, but a root visualized expression that was
			// created by the EE when visualizing the parent, which we don't have access to.

			// As it is, now that we inline the default expansion alongside the addition of the
			// 'UE4 Properties' child, this does seem to work. However, not obvious there is any
			// performance improvement, also not 100% sure it's safe to use the stored evaluation.
			DkmEvaluationResult uobj_eval = null;
			if (expr_.TagValue == DkmVisualizedExpression.Tag.ChildVisualizedExpression)
			{
				uobj_eval = ((DkmChildVisualizedExpression)expr_).EvaluationResult;
			}
			else
			{
				uobj_eval = DefaultEE.DefaultEval(callback_expr_, true);
			}
			eval_ = (DkmSuccessEvaluationResult)uobj_eval;

			DkmEvaluationResult[] children;
			DkmEvaluationResultEnumContext enum_context;
			try
			{
				callback_expr_.GetChildrenCallback(uobj_eval, 0, callback_expr_.InspectionContext, out children, out enum_context);
				// @NOTE: Assuming count will not be large here!!
				callback_expr_.GetItemsCallback(enum_context, 0, enum_context.Count, out children);
			}
			catch
			{
				return;
			}

			uint idx = 0;
			foreach (var child_eval in children)
			{
				if (child_eval.TagValue == DkmEvaluationResult.Tag.SuccessResult)
				{
					var success_eval = child_eval as DkmSuccessEvaluationResult;
					Debug.Assert(success_eval != null);

					DkmExpressionValueHome home;
					if(success_eval.Address != null)
					{
						home = DkmPointerValueHome.Create(success_eval.Address.Value);
                    }
					else
					{
						home = DkmFakeValueHome.Create(0);
					}
					DkmChildVisualizedExpression child = DkmChildVisualizedExpression.Create(
						child_eval.InspectionContext,
						callback_expr_.VisualizerId,
						callback_expr_.SourceId,
						child_eval.StackFrame,
						home,
						child_eval,
						expr_,
						idx,
						null
						);
					MemberExpressions[child_eval.Name] = child;
				}

				++idx;
			}
		}
	}
}
