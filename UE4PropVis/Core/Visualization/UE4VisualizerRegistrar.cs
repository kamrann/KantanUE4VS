using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace UE4PropVis
{
	public static class UE4VisualizerRegistrar
	{
		private static Dictionary<Guid, IUE4VisualizerFactory> visualizers_;

		static UE4VisualizerRegistrar()
		{
			visualizers_ = new Dictionary<Guid, IUE4VisualizerFactory>();
		}

		public static void Register< Factory >(Guid guid) where Factory : IUE4VisualizerFactory, new()
		{
			visualizers_[guid] = new Factory();
		}

		public static bool TryCreateVisualizer(DkmVisualizedExpression expression, out UE4Visualizer visualizer)
		{
			visualizer = null;

			IUE4VisualizerFactory factory = null;
			bool result = visualizers_.TryGetValue(expression.VisualizerId, out factory);
			if (!result)
			{
				return false;
			}

			visualizer = factory.CreateVisualizer(expression);
			return true;
		}
	}
}
