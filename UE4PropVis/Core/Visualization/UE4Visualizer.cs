using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace UE4PropVis
{
    public abstract class UE4Visualizer
	{
		protected DkmVisualizedExpression expression_;

		public UE4Visualizer(DkmVisualizedExpression expression)
		{
			expression_ = expression;
		}

		public abstract void PrepareExpansion(out DkmEvaluationResultEnumContext enumContext);
		public abstract void GetChildItems(DkmEvaluationResultEnumContext enumContext, int start, int count, out DkmChildVisualizedExpression[] items);

		// Declare read only evaluation result property, to be implemented by derived visualizers.
		public abstract DkmEvaluationResult EvaluationResult { get; }

		public abstract bool WantsCustomExpansion { get; }
	}
}
