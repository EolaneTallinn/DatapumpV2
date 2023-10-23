using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataPumpV2.Classes
{
    internal class RegexLibrary
    {
        /// <summary>
        /// This method return the first one regex match
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="text"></param>
        /// <returns>match.Value</returns>
        /// <exception cref="ArgumentException"></exception>
        internal static string GetFirstMatch(string pattern, string text)
        {
            Match match = Regex.Match(text, pattern);
            if (match.Success)
            {
                return match.Value;
            }
            else
            {
                throw new ArgumentException("No match found for the given regex pattern : " + pattern);
            }
        }

        /// <summary>
        /// This method returns the first captured group value from a regex match.
        /// </summary>
        /// <param name="pattern">The regex pattern to match.</param>
        /// <param name="text">The text to search for a match within.</param>
        /// <returns>The value of the first captured group if a match is found else empty string ""</returns>
        /// <exception cref="ArgumentException">Thrown if no match is found.</exception>
        internal static string GetFirstGroup(Regex pattern, string text)
        {
            Match match = pattern.Match(text);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                throw new ArgumentException("No match found for the given regex pattern: " + pattern);
            }
        }

        /// <summary>
        /// This method return the first regex match with its specific group
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static List<string> GetFirstMatchWithGroups(string pattern, string text)
        {
            List<string> result = new List<string>();
            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                foreach (Group group in match.Groups)
                {
                    result.Add(group.Value);
                }
            }
            else
            {
                throw new ArgumentException("No match found for the given regex pattern : " + pattern);
            }
            return result;
        }

        /// <summary>
        /// This method returns a list of regex matches and their corresponding groups
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static List<List<string>> GetAllMatchesWithGroups(Regex pattern, string text)
        {
            List<List<string>> resultList = new List<List<string>>();
            MatchCollection matches = pattern.Matches(text);

            foreach (Match match in matches)
            {
                List<string> matchResult = new List<string>();
                foreach (Group group in match.Groups)
                {
                    matchResult.Add(group.Value);
                }
                resultList.Add(matchResult);
            }

            if (resultList.Count > 0)
            {
                return resultList;
            }
            else
            {
                throw new ArgumentException("No match found for the given regex pattern : " + pattern);
            }
        }
    }
}