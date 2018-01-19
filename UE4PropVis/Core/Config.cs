using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
//using System.ComponentModel.Design;
//using System.Drawing.Design;


namespace UE4PropVis
{
    [Guid(Guids.Component.PropVisConfigString)]
    public class Config : DialogPage
	{
        // @TODO: This is messy. Can't use singleton as VS needs to be able to create one itself.
        // Ideally we access an instance via the package, but a bit confused with proper dependencies
        // - don't want two-way dependency between package and each of the other assemblies.
        private static Config instance;

        public static Config Instance
        {
            get
            {
/*                if (instance == null)
                {
                    instance = new Config();
                }
 */               return instance;
            }

            set
            {
                instance = value;
            }
        }


        // Policies for which properties to show
        public enum PropDisplayPolicyType
		{
            [Display(Name = "Blueprint Variables Only", Description = "Only variables added to a blueprint will be shown in the list.")]
            BlueprintOnly,

            [Display(Name = "All UPROPERTYs", Description = "Both blueprint variables and native UPROPERTYs will be shown.")]
            All,
		};

		// Policies for when to show the property list child in expansions
		public enum PropListDisplayPolicyType
		{
            [Display(Name = "Always Show", Description = "Fastest. No checks are performed, so the 'BP Properties' entry will always be visible even if the object is not a blueprint.")]
            AlwaysShow,                         // Show regardless (fast)

            [Display(Name = "Relevant Object Types Only", Description = "Slower. Does checking at the object level to ensure the 'BP Properties' list might be relevant.")]
            OnlyForRelevantObjectTypes,         // Check object type can have properties (slower)

            [Display(Name = "Only If Visible Properties", Description = "Slowest. Checks al properties in advance, and if nothing would be visible, the entry is hidden.")]
            OnlyIfHasVisibleProperties,			// Fully test for visible properties (slowest)
		};

		private bool uobj_preview_enabled_ = true;
		private bool uclass_preview_enabled_ = true;
		private bool custom_nullobject_preview_ = true;
		private PropListDisplayPolicyType proplist_display_policy_ = PropListDisplayPolicyType.OnlyForRelevantObjectTypes;
		private PropDisplayPolicyType prop_display_policy_ = PropDisplayPolicyType.BlueprintOnly;
		private bool exact_uobject_types_ = true;

		private HashSet<string> HiddenPropertyNames;

		public Config()
		{
			HiddenPropertyNames = new HashSet<string> {
				"UberGraphFrame"
			};
		}

        [Category("Visualization")]
        [DisplayName("Display UObject Preview")]
        [Description("Specialized watch preview with the object and UClass names.")]
        public bool DisplayUObjectPreview
		{
			get { return uobj_preview_enabled_; }
            set { uobj_preview_enabled_ = value; }
		}

        [Category("Visualization")]
        [DisplayName("Display UClass Preview")]
        [Description("Specialized watch preview with the names of both the UClass and its parent UClass.")]
        public bool DisplaySpecializedUClassPreview
		{
			get { return uclass_preview_enabled_; }
            set { uclass_preview_enabled_ = value; }
        }

        [Category("Visualization")]
        [DisplayName("Display Null UObject Preview")]
        [Description("Custom watch preview for null UObject variables.")]
        public bool CustomNullObjectPreview
		{
			get { return custom_nullobject_preview_; }
            set { custom_nullobject_preview_ = value; }
        }

        [Category("Visualization")]
        [DisplayName("Property Filter")]
        [Description("Defines which UE4 properties should be included in the visualization extension.")]
        public PropDisplayPolicyType PropertyDisplayPolicy
		{
			get { return prop_display_policy_; }
            set { prop_display_policy_ = value; }
        }

        [Category("Visualization")]
        [DisplayName("Property List Display Policy")]
        [Description("Determines what checks the extension does to decide whether or not to show the UE4 properties watch window entry. This is just a cosmetic/performance setting.")]
        public PropListDisplayPolicyType PropertyListDisplayPolicy
		{
			get { return proplist_display_policy_; }
            set { proplist_display_policy_ = value; }
        }

        [Category("Visualization")]
        [DisplayName("Exact UObject Types")]
        [Description("For entries in the 'Properties' list that are object properties, if true the 'Type' column will show the specific UClass of the property. When false, the 'Type' column will always show simply 'UObject'. Set to false if experiencing performance issues.")]
        public bool ShowExactUObjectTypes
		{
			get { return exact_uobject_types_; }
            set { exact_uobject_types_ = value; }
        }

        public bool IsPropertyHidden(string prop_name)
		{
			return HiddenPropertyNames.Contains(prop_name);
		}
	}
}
