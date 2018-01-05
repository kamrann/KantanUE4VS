using System;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.Shell;

using MsVsShell = Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace KUE4VS.CodeGeneration
{
    public static class SourceGenerator
    {
        public static string ProcessTextTemplate(
            string tt_filepath,
            Dictionary<string, Object> parameters
            )
        {
            // Get a service provider - how you do this depends on the context:  
            ServiceProvider serviceProvider = new ServiceProvider(
                ExtContext.Instance.Dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            ITextTemplating t4 = serviceProvider.GetService(typeof(STextTemplating)) as ITextTemplating;

            ITextTemplatingSessionHost host = t4 as ITextTemplatingSessionHost;
            // Create a Session in which to pass parameters:  
            host.Session = host.CreateSession();
            // Add parameter values to the Session:
            foreach (var parm in parameters)
            {
                host.Session[parm.Key] = parm.Value;
            }

            var output = ExtContext.Instance.GetOutputPane();
            output.OutputStringThreadSafe("Invoking text template processor...\n");

            // Process a text template:  
            string result = t4.ProcessTemplate(tt_filepath, System.IO.File.ReadAllText(tt_filepath));

            output.OutputStringThreadSafe("Template processing completed.\n");
            return result;
        }

        public static string GenerateHeader(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            bool reflected,
            List<string> nspace,
            string body
            )
        {
            string result = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.hdr_file
            {
                file_title = file_title,
                file_header = file_header,
                default_includes = default_includes,
                reflected = reflected,
                nspace = nspace,
                body = body
            }.TransformText();

            return result;
        }

        public static string GenerateCpp(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            bool matching_header,
            List<string> nspace,
            string body,
            string loctext_ns = null,
            string footer = null
            )
        {
            string result = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.cpp_file
            {
                file_title = file_title,
                file_header = file_header,
                default_includes = default_includes,
                matching_header = matching_header,
                loctext_ns = loctext_ns,
                nspace = nspace,
                body = body,
                footer_content = footer
            }.TransformText();

            return result;
        }

        public static string GenerateTypeHeader(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            string class_keyword,
            bool reflected,
            List<string> nspace,
            string classname,
            string baseclass,
            string modulename,
            bool export
            )
        {
            string reflected_macro = class_keyword == "class" ? "UCLASS()" : "USTRUCT()";

            string decl = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.class_type_decl
            {
                type_name = classname,
                base_class = baseclass,
                module_name = modulename,
                export = export,
                type_keyword = class_keyword,
                reflected = reflected,
                reflection_macro = reflected_macro
            }.TransformText();

            return GenerateHeader(file_title, file_header, default_includes, reflected, nspace, decl);
        }

        public static string GenerateTypeCpp(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            List<string> nspace,
            string classname
            )
        {
            // @TODO:
            const bool matching_header = true;
            string body = "";

            return GenerateCpp(file_title, file_header, default_includes, matching_header, nspace, body);
        }

        public static string GenerateBuildRulesFile(
            string module_name,
            string file_header,
            IEnumerable<string> public_deps,
            IEnumerable<string> private_deps,
            IEnumerable<string> dynamic_deps,
            bool enforce_iwyu,
            bool suppress_unity
            )
        {
            string cpp = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.build_cs_file
            {
                module_name = module_name,
                file_header = file_header,
                public_deps = public_deps,
                private_deps = private_deps,
                dynamic_deps = dynamic_deps,
                enforce_iwyu = enforce_iwyu,
                suppress_unity = suppress_unity,
            }.TransformText();

            return cpp;
        }

        public static string GenerateModuleInterfaceHeader(
            string file_title,
            string file_header,
            string module_name,
            string interface_class_name,
            IEnumerable<string> additional_includes,
            List<string> nspace
            )
        {
            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_interface_decl
            {
                module_name = module_name,
                interface_name = interface_class_name
            }.TransformText();

            List<string> includes = new List<string>();
            includes.Add("ModuleManager.h");
            includes.AddRange(additional_includes);

            return GenerateHeader(file_title, file_header, includes, false, nspace, body);
        }

        public static string GenerateModuleImplHeader(
            string file_title,
            string file_header,
            string module_name,
            string custom_base,
            string custom_base_header,
            IEnumerable<string> additional_includes,
            List<string> nspace
            )
        {
            bool has_custom_base = !String.IsNullOrEmpty(custom_base);
            string base_class = has_custom_base ? custom_base : "IModuleInterface";

            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_impl_decl
            {
                module_name = module_name,
                base_class = base_class,
                custom_base = has_custom_base
            }.TransformText();
            
            List<string> includes = new List<string>();
            if (has_custom_base)
            {
                includes.Add(custom_base_header);
            }
            else
            {
                includes.Add("ModuleManager.h");
            }
            includes.AddRange(additional_includes);

            return GenerateHeader(file_title, file_header, includes, false, nspace, body);
        }

        public static string GenerateModuleImplCpp(
            string file_title,
            string file_header,
            string module_name,
            bool custom_impl,
            string custom_base,
            IEnumerable<string> additional_includes,
            List<string> nspace
            )
        {
            string body = "";

            if (custom_impl)
            {
                bool has_custom_base = !String.IsNullOrEmpty(custom_base);
                string base_class = has_custom_base ? custom_base : "IModuleInterface";

                body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_impl_implementation
                {
                    module_name = module_name,
                    base_class = base_class,
                    custom_base = has_custom_base
                }.TransformText();
            }

            string footer = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_implementation_macro
            {
                module_name = module_name,
                impl_class = custom_impl ? ("F" + module_name + "ModuleImpl") : "FDefaultModuleImpl"
            }.TransformText();

            bool matching_header = custom_impl;
            string loctext_ns = module_name + "ModuleImpl";

            List<string> includes = new List<string>();
            if (!custom_impl)
            {
                includes.Add("ModuleManager.h");
            }
            includes.AddRange(additional_includes);

            return GenerateCpp(file_title, file_header, includes, matching_header, nspace, body, loctext_ns, footer);
        }
    }
}
