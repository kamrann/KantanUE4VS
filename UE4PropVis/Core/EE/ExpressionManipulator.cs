using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UE4PropVis.Core.EE
{
	class ExpressionManipulator
	{
		//		private string ptr_expr_;
		//		private bool needs_deref_;
		private string expr_;

/*		public static ExpressionManipulator FromPointerExpression(string expr)
		{
			return new ExpressionManipulator("(" + expr + ")", false);
		}

		public static ExpressionManipulator FromNonPointerExpression(string expr)
		{
			return new ExpressionManipulator("(&(" + expr + "))", true);
		}

		private ExpressionManipulator(string ptr_expr, bool needs_deref)
		{
			ptr_expr_ = ptr_expr;
			needs_deref_ = needs_deref;
		}
*/
		private ExpressionManipulator(string expr)
		{
			expr_ = expr;
		}

		public static ExpressionManipulator FromExpression(string expr)
		{
			return new ExpressionManipulator("(" + expr + ")");
		}

		private ExpressionManipulator Make(string new_expr)//new_ptr_expr)
		{
			return new ExpressionManipulator("(" + new_expr + ")");
				//("(" + new_ptr_expr + ")", needs_deref_);
		}

		public string Expression
		{
			get
			{
				//return needs_deref_ ? ("*" + ptr_expr_) : ptr_expr_;
				return expr_;
			}
		}

		public ExpressionManipulator PtrMember(string member_name)
		{
			return Make(expr_ + "->" + member_name);
		}

		public ExpressionManipulator DirectMember(string member_name)
		{
			return Make(expr_ + "." + member_name);
		}

		public ExpressionManipulator Deref()
		{
			return ManualPrepend("*");
		}

		public ExpressionManipulator AddressOf()
		{
			return ManualPrepend("&");
		}

		public ExpressionManipulator PtrCast(string type_name)
		{
			return Make("(" + type_name + "*)" + expr_);
		}

		public ExpressionManipulator NonPtrCast(string type_name)
		{
			return AddressOf().PtrCast(type_name).Deref();
		}

		public ExpressionManipulator OffsetBytes(string byte_offset)
		{
			return Make(expr_ + " + " + byte_offset);
		}

		public ExpressionManipulator ManualAppend(string postfix)
		{
			return Make(expr_ + postfix);
		}

		public ExpressionManipulator ManualPrepend(string prefix)
		{
			return Make(prefix + expr_);
		}
	}
}
