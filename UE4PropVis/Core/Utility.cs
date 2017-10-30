using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Debugger.Evaluation;


namespace UE4PropVis
{
	public class BoolEvaluation
	{
		public enum EEvalResult
		{
			False,
			True,
			Indeterminate,
		};

		private EEvalResult result_;

		public bool IsValid
		{
			get { return result_ != EEvalResult.Indeterminate; }
		}

		public bool Value
		{
			get
			{
				if(IsValid)
				{
					return result_ == EEvalResult.False ? false : true;
				}
				else
				{
					throw new Exception();
				}
			}
		}

		public BoolEvaluation(bool val)
		{
			result_ = val ? EEvalResult.True : EEvalResult.False;
		}

		private BoolEvaluation()
		{
			result_ = EEvalResult.Indeterminate;
		}

		public static BoolEvaluation Indeterminate = new BoolEvaluation();
	};

	public static class Utility
	{
		public static string GetExpressionName(DkmVisualizedExpression expression)
		{
			if(expression.TagValue == DkmVisualizedExpression.Tag.RootVisualizedExpression)
			{
				return ((DkmRootVisualizedExpression)expression).Name;
			}
			else if(expression.TagValue == DkmVisualizedExpression.Tag.ChildVisualizedExpression)
			{
				return ((DkmChildVisualizedExpression)expression).EvaluationResult.Name;
			}
			else
			{
				return null;
			}
		}

		public static string GetExpressionFullName(DkmVisualizedExpression expression)
		{
			if (expression.TagValue == DkmVisualizedExpression.Tag.RootVisualizedExpression)
			{
				return ((DkmRootVisualizedExpression)expression).FullName;
			}
			else if (expression.TagValue == DkmVisualizedExpression.Tag.ChildVisualizedExpression)
			{
				return ((DkmChildVisualizedExpression)expression).EvaluationResult.FullName;
			}
			else
			{
				return null;
			}
		}

#if !VS2013
		public static string GetExpressionType(DkmVisualizedExpression expression)
		{
			if (expression.TagValue == DkmVisualizedExpression.Tag.RootVisualizedExpression)
			{
                var temp = expression as DkmRootVisualizedExpression;
				return ((DkmRootVisualizedExpression)expression).Type;
			}
			else if (expression.TagValue == DkmVisualizedExpression.Tag.ChildVisualizedExpression)
			{
				var eval = ((DkmChildVisualizedExpression)expression).EvaluationResult;
				if (eval.TagValue == DkmEvaluationResult.Tag.SuccessResult)
				{
					var success = eval as DkmSuccessEvaluationResult;
					return success.Type;
				}
				else
				{
					return "";
				}
			}
			else
			{
				return null;
			}
		}
#endif

		// Removes any ",..." format specifiers from the end of the given expression string
		public static string StripExpressionFormatting(string expr_str)
		{
			int comma = expr_str.IndexOf(',');
			return comma == -1 ? expr_str : expr_str.Substring(0, comma);
		}

		// unsigned char's have their value formatted as: "<num> '<char>'"
		public static string GetNumberFromUcharValueString(string uchar_val_str)
		{
			int space = uchar_val_str.IndexOf(' ');
			return (space == -1) ? uchar_val_str : uchar_val_str.Substring(0, space);
		}
	}
}
