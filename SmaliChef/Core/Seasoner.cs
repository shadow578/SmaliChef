using SmaliChef.Util;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SmaliChef.Core
{
    /// <summary>
    /// handles flavoring of strings and files
    /// </summary>
    public class Seasoner
    {
        /// <summary>
        /// flavors a stream of text
        /// </summary>
        /// <param name="input">the input stream (eg. source file)</param>
        /// <param name="output">the output file (eg. output file)</param>
        /// <param name="activeFlavors">a list of currently active flavors</param>
        /// <param name="logSession">log session for logging</param>
        public void FlavorStream(StreamReader input, StreamWriter output, List<string> activeFlavors, Log.AsyncLogSession logSession)
        {
            //log processing the file
            logSession?.v($"Start processing stream...");

            //process line by line
            string ln;
            int lnCount = 0;
            while ((ln = input.ReadLine()) != null)
            {
                //set logging tag
                logSession?.PushTag($"{logSession?.GetTag()}:{lnCount++}");

                //flavor string and write to output
                ln = FlavorString(ln, activeFlavors, logSession);
                output.WriteLine(ln);

                //pop back tag
                logSession?.PopTag();
            }
        }

        /// <summary>
        /// replaces all flavor statements with the correct flavor content in this string
        /// </summary>
        /// <param name="s">the string to flavor</param>
        /// <param name="activeFlavors">a list of active flavors</param>
        /// <param name="logSession">log session for logging</param>
        /// <returns>the flavored string</returns>
        public string FlavorString(string s, List<string> activeFlavors, Log.AsyncLogSession logSession)
        {
            //repeatedly replace statements with their flavor content
            Match statementMatch = null;
            Dictionary<string, string> flavors;
            do
            {
                //match string using regex, match the first statement
                statementMatch = Regex.Match(s, FLAVOR_STATEMENT_PATTERN);
                if (!statementMatch.Success) continue;

                //get full statement AND expression from match
                string statement = statementMatch.Value;
                string expression = statementMatch.Groups[1].Value;
                logSession?.vv($"process statement {statement}");

                //skip if expression is empty
                if (string.IsNullOrWhiteSpace(expression)) break;

                //get flavors for expression, skip if there are none
                flavors = GetFlavorsFromExpression(expression, logSession);
                if (flavors == null || flavors.Count <= 0) break;

                //get the correct flavor to use, skip if none of the flavors is valid
                string flavor = GetFirstFlavor(flavors.Keys, activeFlavors);

                //get the right content for the flavor, default to string.Empty
                string flavorContent = string.Empty;
                if (string.IsNullOrWhiteSpace(flavor))
                {
                    logSession?.w($"no active flavor for {statement}, fallback to empty");
                }
                else
                {
                    flavorContent = flavors[flavor];
                    if (string.IsNullOrWhiteSpace(flavorContent))
                    {
                        logSession?.w($"flavor content for flavor {flavor} is empty!");
                    }
                }

                //replace flavor statement with content
                logSession?.d($"replacing {statement} with {flavorContent}...");
                s = s.ReplaceFirst(statement, flavorContent);
            }
            while (statementMatch.Success);
            return s;
        }

        #region internal functions
        /// <summary>
        /// matches a flavor statement, captures the expression in CG1
        /// #[flavor]<EXPRESSION>#[/flavor]
        /// </summary>
        const string FLAVOR_STATEMENT_PATTERN = @"#\[flavor\](.{1,})#\[\/flavor\]";

        /// <summary>
        /// matches separate flavor expressions, captures flavor name in CG1 and flavor content in CG2
        /// <FLAVOR_NAME>:"<FLAVOR_CONTENT>"
        /// </summary>
        const string FLAVOR_EXPRESSION_PATTERN = @"([a-z]{1,}):""([^""]{1,})"";";

        /// <summary>
        /// splits the expression list into flavor/content pairs
        /// </summary>
        /// <param name="expression">the expression to split into pairs</param>
        /// <returns>the flavor/content pairs</returns>
        Dictionary<string/*flavor name*/, string/*flavor content*/> GetFlavorsFromExpression(string expression, Log.AsyncLogSession logSession)
        {
            //init dictionary to hold results
            Dictionary<string, string> flavorDict = new Dictionary<string, string>();

            //match all expressions
            MatchCollection expressionMatches = Regex.Matches(expression, FLAVOR_EXPRESSION_PATTERN);
            logSession?.d($"Processing expressions for {expression}, found {expressionMatches.Count} matches.");
            if (expressionMatches.Count <= 0) return null;

            //enumerate all expressionss
            foreach (Match expressionMatch in expressionMatches)
            {
                //skip if no succcess or CG1 or CG2 are not valid
                if (!expressionMatch.Success
                    || string.IsNullOrWhiteSpace(expressionMatch.Groups[1].Value)
                    || expressionMatch.Groups[2].Value == null) continue;

                //get flavor name and flavor content
                string flavorName = expressionMatch.Groups[1].Value;
                string flavorContent = expressionMatch.Groups[2].Value;

                //do some processing with the strings
                flavorName = flavorName.ToLower().Trim();
                flavorContent = flavorContent.Trim();

                //skip if dict already contains this flavor name
                if (flavorDict.ContainsKey(flavorName))
                {
                    logSession?.w($"Duplicate flavor {flavorName} in expression {expression}!");
                    continue;
                }

                //add to dict
                logSession?.v($"add {flavorName} - {flavorContent} to dict");
                flavorDict.Add(flavorName, flavorContent);
            }

            //return null if dict is empty
            logSession?.v($"flavorDict.Count is {flavorDict.Count}");
            if (flavorDict.Count <= 0) return null;

            //return filled dict
            return flavorDict;
        }

        /// <summary>
        /// Get the first of the available flavors that is also in the active flavors list
        /// </summary>
        /// <param name="availableFlavors">list of all available flavors</param>
        /// <param name="activeFlavors">list of all active flavors</param>
        /// <returns>the first available flavor that is also active</returns>
        string GetFirstFlavor(Dictionary<string, string>.KeyCollection availableFlavors, List<string> activeFlavors)
        {
            foreach (string flavor in availableFlavors)
            {
                if (activeFlavors.Contains(flavor)) return flavor;
            }

            return null;
        }
        #endregion
    }
}
