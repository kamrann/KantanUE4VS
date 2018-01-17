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
            string body
            )
        {
            string result = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.hdr_file
            {
                file_title = file_title,
                file_header = file_header,
                default_includes = default_includes,
                reflected = reflected,
                body = body
            }.TransformText();

            return result;
        }

        public static string GenerateCpp(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            bool matching_header,
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
                reflection_macro = reflected_macro,
                constructor = true
            }.TransformText();

            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
            {
                nspace = nspace,
                content = decl
            }.TransformText();

            return GenerateHeader(file_title, file_header, default_includes, reflected, body);
        }

        public static string GenerateUInterfaceHeader(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            string interfacename,
            string modulename,
            bool export
            )
        {
            string reflected_macro = "UINTERFACE()";
            string uint_decl = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.class_type_decl
            {
                type_name = "U" + interfacename,
                base_class = "UInterface",
                module_name = modulename,
                export = export,
                type_keyword = "class",
                reflected = true,
                reflection_macro = reflected_macro,
                constructor = false
            }.TransformText();

            var declarations = new List<string>
            {
                "\t// Interface methods",
                "public:",
            };

            string iint_decl = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.class_type_decl
            {
                type_name = "I" + interfacename,
                base_class = null,
                module_name = modulename,
                export = export,
                type_keyword = "class",
                reflected = true,
                reflection_macro = null,
                constructor = false,
                declarations = declarations
            }.TransformText();

            string body = uint_decl + "\r\n" + iint_decl;

            return GenerateHeader(file_title, file_header, default_includes, true, body);
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
            string content = "";

            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
            {
                nspace = nspace,
                content = content
            }.TransformText();

            return GenerateCpp(file_title, file_header, default_includes, matching_header, body);
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
            string decl = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_interface_decl
            {
                module_name = module_name,
                interface_name = interface_class_name
            }.TransformText();

            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
            {
                nspace = nspace,
                content = decl
            }.TransformText();

            List<string> includes = new List<string>();
            includes.Add("ModuleManager.h");
            includes.AddRange(additional_includes);

            return GenerateHeader(file_title, file_header, includes, false, body);
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

            string decl = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_impl_decl
            {
                module_name = module_name,
                base_class = base_class,
                custom_base = has_custom_base
            }.TransformText();

            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
            {
                nspace = nspace,
                content = decl
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

            return GenerateHeader(file_title, file_header, includes, false, body);
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
            string content = "";

            if (custom_impl)
            {
                bool has_custom_base = !String.IsNullOrEmpty(custom_base);
                string base_class = has_custom_base ? custom_base : "IModuleInterface";

                content = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.module_impl_implementation
                {
                    module_name = module_name,
                    base_class = base_class,
                    custom_base = has_custom_base
                }.TransformText();
            }

            string body = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
            {
                nspace = nspace,
                content = content
            }.TransformText();

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

            return GenerateCpp(file_title, file_header, includes, matching_header, body, loctext_ns, footer);
        }

        public static string GenerateUPluginFile(
            string plugin_name,
            string category,
            bool with_content
            )
        {
            string json = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.uplugin_file
            {
                plugin_name = plugin_name,
                category = category,
                with_content = with_content,
            }.TransformText();

            return json;
        }
    }
}
