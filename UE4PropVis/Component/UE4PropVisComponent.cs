using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;

using UE4PropVis.Core;
using UE4PropVis.Core.EE;


namespace UE4PropVis
{
	static class Impl
	{
		public static ObjectContext.Factory ObjCtxFactory = new EvaluatedObjectContext.MyFactory();
	};

    class UE4PropVisComponent : IDkmCustomVisualizer
    {
		static UE4PropVisComponent()
        {
			UE4VisualizerRegistrar.Register< UObjectVisualizer.Factory >(Guids.Visualizer.UObject);
			//UE4VisualizerRegistrar.Register< PropertyListVisualizer.Factory >(Guids.Visualizer.PropertyList);
		}

		void OnVisualizerMatchFailed(DkmVisualizedExpression expression, out DkmEvaluationResult result)
		{
			result = DkmFailedEvaluationResult.Create(
				expression.InspectionContext,
				expression.StackFrame,
				Utility.GetExpressionName(expression),
				Utility.GetExpressionFullName(expression),
				String.Format("UE4PropVis: No visualizer is registered for VisualizerId {0}", expression.VisualizerId),
				DkmEvaluationResultFlags.Invalid,
				null
				);
		}

		void IDkmCustomVisualizer.EvaluateVisualizedExpression(DkmVisualizedExpression expression, out DkmEvaluationResult resultObject)
		{
            if (KUE4VS.ExtContext.Instance.ExtensionOptions.EnablePropVis == false)
            {
                var LangExpr = DkmLanguageExpression.Create(DefaultEE.CppLanguage, DkmEvaluationFlags.None, Utility.GetExpressionFullName(expression), null);
                expression.EvaluateExpressionCallback(expression.InspectionContext, LangExpr, expression.StackFrame, out resultObject);
                return;
            }

			Debug.Print("UE4PV: EvaluateVisualizedExpression('{0}'/'{1}', [{2}])",
				Utility.GetExpressionFullName(expression),
				Utility.GetExpressionName(expression),
				expression.TagValue
				);

			// Sanity check to confirm this is only being invoked for UObject types. @TODO: Remove eventually.
			// Believe this method is only invoked on DkmRootVisualizedExpression instances, not children.
			Debug.Assert(expression.VisualizerId == Guids.Visualizer.UObject);

			UE4Visualizer visualizer = null;
			bool result = UE4VisualizerRegistrar.TryCreateVisualizer(expression, out visualizer);
			if(!result)
			{
				OnVisualizerMatchFailed(expression, out resultObject);
				return;
			}

			// Evaluate the visualization result
			DkmEvaluationResult eval = visualizer.EvaluationResult;
			resultObject = eval;

			// Associate the visualizer with the expression
			expression.SetDataItem<ExpressionDataItem>(
				DkmDataCreationDisposition.CreateAlways,
				new ExpressionDataItem(visualizer)
				);
		}

        void IDkmCustomVisualizer.GetChildren(DkmVisualizedExpression expression, int initialRequestSize, DkmInspectionContext inspectionContext, out DkmChildVisualizedExpression[] initialChildren, out DkmEvaluationResultEnumContext enumContext)
        {
			Debug.Print("UE4PV: GetChildren('{0}'/'{1}', [{2}, {3}])",
				Utility.GetExpressionFullName(expression),
				Utility.GetExpressionName(expression),
				expression.TagValue,
				expression.VisualizerId
				);
			

			var data_item = expression.GetDataItem<ExpressionDataItem>();
			var visualizer = data_item.Visualizer;
			Debug.Assert(visualizer != null);

			visualizer.PrepareExpansion(out enumContext);
			initialChildren = new DkmChildVisualizedExpression[0];
		}

        void IDkmCustomVisualizer.GetItems(DkmVisualizedExpression expression, DkmEvaluationResultEnumContext enumContext, int startIndex, int count, out DkmChildVisualizedExpression[] items)
        {
			var data_item = expression.GetDataItem<ExpressionDataItem>();
			var visualizer = data_item.Visualizer;
			Debug.Assert(visualizer != null);

			visualizer.GetChildItems(enumContext, startIndex, count, out items);
        }

        string IDkmCustomVisualizer.GetUnderlyingString(DkmVisualizedExpression visualizedExpression)
        {
            throw new NotImplementedException();
        }

        void IDkmCustomVisualizer.SetValueAsString(DkmVisualizedExpression visualizedExpression, string value, int timeout, out string errorText)
        {
            throw new NotImplementedException();
        }

        void IDkmCustomVisualizer.UseDefaultEvaluationBehavior(DkmVisualizedExpression expression, out bool useDefaultEvaluationBehavior, out DkmEvaluationResult defaultEvaluationResult)
        {
			Debug.Print("UE4PV: UseDefaultEvaluationBehavior('{0}'/'{1}', [{2}, {3}])",
				Utility.GetExpressionFullName(expression),
				Utility.GetExpressionName(expression),
				expression.TagValue,
				expression.VisualizerId
				);

            if (KUE4VS.ExtContext.Instance.ExtensionOptions.EnablePropVis)
            {
                var data_item = expression.GetDataItem<ExpressionDataItem>();
                if (data_item != null)
                {
                    Debug.Assert(data_item.Visualizer != null);

                    if (data_item.Visualizer.WantsCustomExpansion)
                    {
                        useDefaultEvaluationBehavior = false;
                        defaultEvaluationResult = null;
                        return;
                    }
                }
            }

			// Don't need any special expansion, just delegate back to the default EE
			useDefaultEvaluationBehavior = true;

			/* @NOTE:
			Not sure where exactly the problem is, but UObject properties don't expand in VS 2013.
			When we try, there is an initial call to this method with the property's child expr,
			so we come here, and do a default eval, which, if the prop is a UObject, will invoke 
			EvaluateVisualizedExpression above with a new root expr. In 2015, that is followed by
			another call to this method for the root expr, which has an attached visualizer and we
			do the custom expansion. In 2013, it seems the second call into this method does not
			occur for some reason.
			*/
			defaultEvaluationResult = DefaultEE.DefaultEval(expression, false);
/*
	Doing this will crash VS!
	DkmSuccessEvaluationResult.Create(null, null, "", "", DkmEvaluationResultFlags.None,
				"", "", "", DkmEvaluationResultCategory.Other, DkmEvaluationResultAccessType.None,
				DkmEvaluationResultStorageType.None, DkmEvaluationResultTypeModifierFlags.None,
				null, null, null, null);
*/		}
	}
}
