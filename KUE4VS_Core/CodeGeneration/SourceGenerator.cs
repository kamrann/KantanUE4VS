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

        public static string GenerateTypeHeader(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            string class_keyword,
            bool reflected,
            string classname,
            string baseclass,
            string modulename,
            bool export
            )
        {
            string reflected_macro = class_keyword == "class" ? "UCLASS()" : "USTRUCT()";

            Dictionary<string, Object> parameters = new Dictionary<string, object>();
            parameters["item_name"] = classname;
            parameters["base_class"] = baseclass;
            parameters["module_name"] = modulename;
            parameters["export"] = export;
            parameters["class_keyword"] = class_keyword;
            parameters["reflected"] = reflected;
            parameters["reflection_macro"] = reflected_macro;

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

            // todo
            var namespace_defn = new List<string> { "foo", "bar", "baz" };

            parameters["file_title"] = file_title;
            parameters["file_header"] = file_header;
            parameters["default_includes"] = default_includes;
            parameters["namespace"] = namespace_defn;
            parameters["body"] = decl;

            string result = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.hdr_file
            {
                file_title = file_title,
                file_header = file_header,
                default_includes = default_includes,
                nspace = namespace_defn,
                body = decl
            }.TransformText();

            return result;
        }

        public static string GenerateTypeCpp(
            string file_title,
            string file_header,
            IEnumerable<string> default_includes,
            string classname
            )
        {
            string body = "";

            string cpp = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.cpp_file
            {
                file_title = file_title,
                file_header = file_header,
                matching_header = true,
                default_includes = default_includes,
                body = body
            }.TransformText();

            return cpp;
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
    }
}
