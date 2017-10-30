using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Debugger;


// Data item added only to UObject expressions.
namespace UE4PropVis.Core
{
	class UObjectDataItem : DkmDataItem
	{
		bool is_pointer_;
		bool is_null_;

		public UObjectDataItem(bool is_ptr, bool is_null)
		{
			is_pointer_ = is_ptr;
			is_null_ = is_null;
		}

		public bool IsPointer
		{
			get { return is_pointer_; }
		}

		public bool IsNull
		{
			get { return is_null_; }
		}
	}
}
