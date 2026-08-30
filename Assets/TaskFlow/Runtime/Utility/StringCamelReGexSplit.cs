using System.Text.RegularExpressions;

namespace TaskFlow.Utility
{
    public static class StringCamelReGexSplit
    {
        private static readonly Regex CamelCaseRegex = new Regex(
            @"(?<=[a-z])(?=[A-Z])|(?<!^)(?=[A-Z][a-z])",
            RegexOptions.Compiled);

        public static string SplitCamelCase(this string input) =>
            string.IsNullOrEmpty(input) ? input : CamelCaseRegex.Replace(input, " ");
    }
}