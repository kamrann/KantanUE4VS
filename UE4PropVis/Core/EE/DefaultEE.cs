// Copyright 2017-2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Debugger.Evaluation;


namespace UE4PropVis
{
	public class DefaultEE
	{
		public static DkmLanguage CppLanguage = DkmLanguage.Create("C++", new DkmCompilerId(Guids.Vendor.Microsoft, Guids.Language.Cpp));

		public static DkmEvaluationResult DefaultEval(string text, DkmVisualizedExpression expression, bool raw_format)
		{
			if (raw_format && text.Length > 0)
			{
                // Ensure we have the format specifier for raw format, to prevent the evaluator just calling back again to us.
                text = text.TrimEnd(new char[] { ' ' });
                if (text.Last() == ',')
                {
                    text += '!';
                }
				else
                {
					text += ",!";
				}
			}

			var LangExpr = DkmLanguageExpression.Create(CppLanguage, DkmEvaluationFlags.None, text, null);

			DkmEvaluationResult result;
			try
			{
				expression.EvaluateExpressionCallback(expression.InspectionContext, LangExpr, expression.StackFrame, out result);
			}
			catch(Exception e)
			{
				result = null;
			}
			return result;
		}

		public static DkmEvaluationResult DefaultEval(DkmVisualizedExpression expression, bool raw_format)
		{
			string expr_text = Utility.GetExpressionFullName(expression);
			return DefaultEval(expr_text, expression, raw_format);
		}

	}
}
