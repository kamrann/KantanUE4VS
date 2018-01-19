// Copyright 2017-2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;


namespace UE4PropVis.Core
{
	class DefaultEnumContextDataItem : DkmDataItem
	{
		private DkmEvaluationResultEnumContext default_enum_ctx_;

		public DefaultEnumContextDataItem(DkmEvaluationResultEnumContext ctx)
		{
			default_enum_ctx_ = ctx;
		}

		public DkmEvaluationResultEnumContext Context
		{
			get { return default_enum_ctx_; }
		}
	}
}
