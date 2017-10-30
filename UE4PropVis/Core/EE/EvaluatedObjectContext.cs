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
	/*
	This implementation works by constructing a string expression for each new target context,
	and passing it to the EE for direct evaluation.
	*/
	class EvaluatedObjectContext : ObjectContext
	{
		protected DkmSuccessEvaluationResult eval_;
		protected bool is_pointer_;
		protected string ptr_expr_stub_;

		public class MyFactory : ObjectContext.Factory
		{
			public ObjectContext CreateObjectContext(DkmVisualizedExpression expr, DkmVisualizedExpression callback_expr)
			{
				return new EvaluatedObjectContext(expr, callback_expr);
			}
        };

		public EvaluatedObjectContext(DkmVisualizedExpression expr, DkmVisualizedExpression callback_expr): base(expr, callback_expr)
		{
			DkmEvaluationResult eval = null;
			if (expr_.TagValue == DkmVisualizedExpression.Tag.ChildVisualizedExpression)
			{
				eval = ((DkmChildVisualizedExpression)expr_).EvaluationResult;
			}
			else
			{
				eval = DefaultEE.DefaultEval(expr_, true);
			}
			eval_ = (DkmSuccessEvaluationResult)eval;

			// @NOTE: Pretty sure this is reliable
			is_pointer_ = eval_.Type.Contains('*');

			string fullname = Utility.GetExpressionFullName(expr_);
			// Remove any trailing format specifiers.
			int comma = fullname.IndexOf(',');
			if(comma != -1)
			{
				fullname = fullname.Substring(0, comma);
			}
            string base_expr_stub_ = String.Format(
				"({0})",
				fullname
				);
			ptr_expr_stub_ = is_pointer_ ?
				base_expr_stub_ : String.Format("(&{0})", base_expr_stub_);
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
			string member_expr_str = String.Format(
				"{0}->{1}",
				ptr_expr_stub_,
				name
				);

			return CreateNewExpression(member_expr_str);
		}

		public override DkmChildVisualizedExpression GetBaseClass(string class_name)
		{
			return GetAncestorClass(class_name);
		}

		public override DkmChildVisualizedExpression GetAncestorClass(string class_name)
		{
			string base_expr_str = String.Format(
				"({0}*){1}",
				class_name,
				ptr_expr_stub_
				);

			return CreateNewExpression(base_expr_str);
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
		private DkmChildVisualizedExpression CreateNewExpression(string expr_str)
		{
			DkmSuccessEvaluationResult result_eval = null;
			try
			{
				result_eval = DefaultEE.DefaultEval(expr_str, callback_expr_, true) as DkmSuccessEvaluationResult;
			}
			catch(Exception e)
			{ }

			if (result_eval == null)
			{
				return null;
			}

			DkmExpressionValueHome home;
			if (result_eval.Address != null)
			{
				home = DkmPointerValueHome.Create(result_eval.Address.Value);
			}
			else
			{
				home = DkmFakeValueHome.Create(0);
			}

			// @TODO: This is weird. Can I perhaps construct a DkmRootVisualizedExpression instead??
			// Not sure about a couple of the parameters needed to do so, Module especially...
			return DkmChildVisualizedExpression.Create(
				result_eval.InspectionContext,
				expr_.VisualizerId,
				expr_.SourceId,
				result_eval.StackFrame,
				home,
				result_eval,
				callback_expr_,//expr_,
				0,  // ??
				null
				);
		}
	}
}
