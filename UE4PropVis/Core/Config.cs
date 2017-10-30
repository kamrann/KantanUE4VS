using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UE4PropVis
{
	public static class Config
	{
		// Policies for which properties to show
		public enum PropDisplayPolicyType
		{
			BlueprintOnly,
			All,
		};

		// Policies for when to show the property list child in expansions
		public enum PropListDisplayPolicyType
		{
			AlwaysShow,							// Show regardless (fast)
			OnlyForRelevantObjectTypes,			// Check object type can have properties (slower)
			OnlyIfHasVisibleProperties,			// Fully test for visible properties (slowest)
		};

		// @TODO: runtime configuarable
		private const bool uobj_preview_enabled_ = true;
		private const bool uclass_preview_enabled_ = true;
		private const bool custom_nullobject_preview_ = true;
		private const PropListDisplayPolicyType proplist_display_policy_ = PropListDisplayPolicyType.OnlyForRelevantObjectTypes;
		private const PropDisplayPolicyType prop_display_policy_ = PropDisplayPolicyType.BlueprintOnly;
		private const bool exact_uobject_types_ = true;

		private static HashSet<string> HiddenPropertyNames;

		static Config()
		{
			HiddenPropertyNames = new HashSet<string> {
				"UberGraphFrame"
			};
		}

		public static bool DisplayUObjectPreview
		{
			get
			{
				return uobj_preview_enabled_;
			}
		}

		public static bool DisplaySpecializedUClassPreview
		{
			get
			{
				return uclass_preview_enabled_;
			}
		}

		public static bool CustomNullObjectPreview
		{
			get
			{
				return custom_nullobject_preview_;
			}
		}

		public static PropDisplayPolicyType PropertyDisplayPolicy
		{
			get
			{
				return prop_display_policy_;
			}
		}

		public static PropListDisplayPolicyType PropertyListDisplayPolicy
		{
			get
			{
				return proplist_display_policy_;
			}
		}

		public static bool ShowExactUObjectTypes
		{
			get
			{
				return exact_uobject_types_;
			}
		}

		public static bool IsPropertyHidden(string prop_name)
		{
			return HiddenPropertyNames.Contains(prop_name);
		}
	}
}
