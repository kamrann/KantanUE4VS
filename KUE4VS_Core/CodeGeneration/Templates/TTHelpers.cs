using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUE4VS_Core.CodeGeneration.Templates
{
    public static class TTHelpers
    {
        public static string ConvertBool(bool value)
        {
            return value.ToString().ToLower();
        }

        public static string StringBefore(string prepend, string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            return prepend + value;
        }
    }
}
