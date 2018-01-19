// Copyright 2017-2018 Cameron Angus. All Rights Reserved.

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
	/* @NOTE:
	This class  provides funcionality to access expression evaluations for class members by name.

	!!! Implementation is really a workaround !!!
	It calls back to the default EE to enumerate all children of the base expression, storing
	results in a map for access by name later. Suspect there is some way to directly access
	relative property data (perhaps somehow can get a IDebugProperty3 interface?) which would
	avoid doing a full enumeration.
	*/
	abstract class ObjectContext
	{
		protected DkmVisualizedExpression expr_;
		protected DkmVisualizedExpression callback_expr_;

		public ObjectContext(DkmVisualizedExpression expr, DkmVisualizedExpression callback_expr)
		{
			expr_ = expr;
			callback_expr_ = callback_expr;
		}

		public DkmVisualizedExpression Expression
		{
			get
			{
				return expr_;
			}
		}

		public DkmVisualizedExpression CallbackExpression
		{
			get
			{
				return callback_expr_;
			}
		}

		public interface Factory
		{
			ObjectContext CreateObjectContext(DkmVisualizedExpression expr, DkmVisualizedExpression callback_expr);
		};

		public abstract Factory GetFactory { get; }

		// Returns the result of evaluating the object expression
		public abstract DkmSuccessEvaluationResult GetEvaluationResult();
		// Returns a string representing the name of the C++ class of the object
		public abstract string GetClassName();
		// Returns an expression for a class member, identified by its name
		public abstract DkmChildVisualizedExpression GetMember(string name);
		// Returns expression for the given immediate base
		public abstract DkmChildVisualizedExpression GetBaseClass(string class_name);
		// Returns expression for ancestor class anywhere up the hierarchy.
		public abstract DkmChildVisualizedExpression GetAncestorClass(string class_name);
		// Returns expression for the most derived form of the object
//		public abstract DkmChildVisualizedExpression GetMostDerived();

		// Returns a new context for the specified member
		public ObjectContext GetMemberContext(string name)
		{
			var mb_expr = GetMember(name);
			return mb_expr != null ? GetFactory.CreateObjectContext(mb_expr, callback_expr_) : null;
		}

		// Returns a new context for the immediate base class
		public ObjectContext GetBaseClassContext(string class_name)
		{
			var b_expr = GetBaseClass(class_name);
			return b_expr != null ? GetFactory.CreateObjectContext(b_expr, callback_expr_) : null;
		}

		// Returns a new context for the specified ancestor part of the object
		public ObjectContext GetAncestorClassContext(string class_name)
		{
			var a_expr = GetAncestorClass(class_name);
			return a_expr != null ? GetFactory.CreateObjectContext(a_expr, callback_expr_) : null;
		}

		// Returns a new context for the most derived form of the object
/*		public ObjectContext GetMostDerivedContext()
		{
			var md_expr = GetMostDerived();
			return md_expr != null ? GetFactory.CreateObjectContext(md_expr) : null;
		}
*/	}
}
