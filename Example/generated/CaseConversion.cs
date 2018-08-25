//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by Odapter on Sun, 03 Jun 2018 01:47:51 GMT.
//     Direct edits will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
//    Odapter - a C# code generator for Oracle packages
//    Copyright(C) 2018 Clay Lipscomb
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Odapter {
    /// <summary>
    /// Converts between various "cases" like camelCase, PascalCase, underscore_delimited
    /// </summary>
    public class CaseConverter {
        #region Constant and Static members
        private const char UNDERSCORE = '_';
        private const string UNDERSCORECHAR_WORD = "underscorechar";
        private static TextInfo _textInfo = new CultureInfo("en-US", false).TextInfo;
        #endregion

        #region Case Conversion Methods
        /// <summary>
        /// Modifies string by inserting underscore before each uppercase letter. Case of letters is not changed.
        /// </summary>
        /// <param name="oldText">Any string. Should have uppercase letters like PascalCase or camelCase.</param>
        /// <returns></returns>
        private static string DelimitCapitalizedWordsWithUnderscore(String oldText) {
            if (String.IsNullOrEmpty(oldText)) return string.Empty;
            char[] oldTextChar = oldText.Trim().ToCharArray(); // get all characters
            string newText = string.Empty;

            // start our first word with first character, regardless of it case
            string word = oldTextChar[0].ToString(); 

            // loop remaining chars searching for uppercase
            for (int i = 1; i < oldTextChar.Length; i++) {
                if (char.IsUpper(oldTextChar[i])) {
                    newText += word; // current word is finished, so append to result
                    word = UNDERSCORE + oldTextChar[i].ToString(); // start new word
                } else { // char is lower case
                    word += oldTextChar[i].ToString(); // add next character to current word
                }
            }
            newText += word; // add the last word
            return newText;
        }

        /// <summary>
        /// Convert a camellCase string to lower-case underscore_delimited
        /// </summary>
        /// <param name="oldText">A camelCase string</param>
        /// <returns>underscore deliminted string</returns>
        public static string ConvertCamelCaseToUnderscoreDelimited(String oldText) {
            return DelimitCapitalizedWordsWithUnderscore(oldText).ToLower();
        }

        /// <summary>
        /// Convert a PascalCase string to lower-case underscore_delimited
        /// </summary>
        /// <param name="oldText">A PascalCase string</param>
        /// <returns>underscore delimited string</returns>
        public static string ConvertPascalCaseToUnderscoreDelimited(String oldText) {
            return DelimitCapitalizedWordsWithUnderscore(oldText).ToLower();
        }

        /// <summary>
        /// Convert an underscore_delimited string to PascalCase
        /// </summary>
        /// <param name="oldText"></param>
        /// <returns></returns>
        private static string ConvertUnderscoreDelimitedToPascalCase(String oldText, bool preserveLeadingAndTrailingUnderscores) {
            if (String.IsNullOrEmpty(oldText)) return string.Empty;
            string newText = oldText.Trim();

            // treat any special characters like delimiters
            newText = Regex.Replace(newText, "[^0-9a-zA-Z]+", UNDERSCORE.ToString());

            string[] token = newText.ToLower().Trim().Split(UNDERSCORE);
            newText = string.Empty;
            foreach (string t in token) newText += _textInfo.ToTitleCase(t);

            // We must guarantee uniqueness with leading/trailing underscores. In other words, if you 
            //  begin or end an underscore-delimited name with an underscore, we will either preserve
            //  the underscore as a character (non-standard for PascalCase) or replace it with the word 
            //  "Underscore" to keep a pure PascalCase (preferred).
            if (!preserveLeadingAndTrailingUnderscores && oldText.EndsWith(UNDERSCORE.ToString())) newText += ConvertToCapitalized(UNDERSCORECHAR_WORD);
            if (!preserveLeadingAndTrailingUnderscores && oldText.StartsWith(UNDERSCORE.ToString())) newText = ConvertToCapitalized(UNDERSCORECHAR_WORD) + newText;
            return newText;
        }

        public static string ConvertUnderscoreDelimitedToPascalCase(String oldText) {
            return ConvertUnderscoreDelimitedToPascalCase(oldText, false);
        }

        /// <summary>
        /// Convert an underscore_delimited string to camelCase
        /// </summary>
        /// <param name="oldText"></param>
        /// <returns>camelCase string</returns>
        private static string ConvertUnderscoreDelimitedToCamelCase(String oldText, bool preserveLeadingAndTrailingUnderscores) {
            if (String.IsNullOrEmpty(oldText)) return string.Empty;
            String pascalCase = ConvertUnderscoreDelimitedToPascalCase(oldText, preserveLeadingAndTrailingUnderscores);
            return pascalCase.Substring(0, 1).ToLower() + (pascalCase.Length == 1 ? string.Empty : pascalCase.Substring(1));
        }

        public static string ConvertUnderscoreDelimitedToCamelCase(String oldText) {
            return ConvertUnderscoreDelimitedToCamelCase(oldText, false);
        }

        /// <summary>
        /// Convert underscored_delimited string into a Title Case 
        /// </summary>
        /// <param name="columnName">underscore deliminted string</param>
        /// <returns>title case label</returns>
        public static string ConvertUnderscoreDelimitedToLabel(string columnName) {
            // assume words are delimited by underscore
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(columnName.ToLower().Replace(UNDERSCORE.ToString(), " "));
        }

        /// <summary>
        /// Convert string to title case 
        /// </summary>
        /// <param name="columnName">underscore delimited string</param>
        /// <returns>title case label</returns>
        public static string ConvertToCapitalized(string value) {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            value = value.Trim();
            return char.ToUpper(value[0]) + (value.Length > 1 ? value.Substring(1).ToLower() : "");
        }

        /// <summary>
        /// Convert an underscore_delimited string to _camelCasePrefixedWithUnderscore
        /// </summary>
        /// <param name="oldText"></param>
        /// <returns></returns>
        private static string ConvertUnderscoreDelimitedToCamelCasePrefixedWithUnderscore(String oldText, bool preserveTrailingUnderscore) {
            return UNDERSCORE + ConvertUnderscoreDelimitedToCamelCase(oldText, preserveTrailingUnderscore);
        }

        public static string ConvertUnderscoreDelimitedToCamelCasePrefixedWithUnderscore(String oldText) {
            return ConvertUnderscoreDelimitedToCamelCase(oldText, false);
        }
        #endregion
    }
}