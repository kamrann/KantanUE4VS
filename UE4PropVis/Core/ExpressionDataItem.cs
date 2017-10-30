using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Debugger;


namespace UE4PropVis.Core
{
	class ExpressionDataItem : DkmDataItem
	{
		UE4Visualizer vis_;

		public ExpressionDataItem(UE4Visualizer vis)
		{
			vis_ = vis;
		}

		public UE4Visualizer Visualizer
		{
			get { return vis_; }
		}
	}
}
