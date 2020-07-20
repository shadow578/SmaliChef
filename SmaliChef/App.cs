using SmaliChef.Core;
using SmaliChef.Core.Filters;
using SmaliChef.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmaliChef
{
    public class App
    {
        public static void Main(string[] args)
        {

#if DEBUG
            if (args.Length <= 0)
            {
                //override args on debug builds
                Console.Write("DEBUG Build, enter launch args (NO Spaces in args): flavor.exe ");
                string argsR = Console.ReadLine();
                if (!string.IsNullOrEmpty(argsR))
                {
                    args = argsR.Split(' ');
                }
                Console.Title = argsR;
            }
#endif

            if (args.Length <= 0 || args[0].Equals("-?"))
            {
                ShowHelp();
                return;
            }

            //set logging flags
            bool debugLog = false,
                verboseLog = false,
                vverboseLog = false,
                copyAll = false,
                markedFilesOnly = false,
                processParallel = false;
            ParseFlags(args, ref debugLog, ref verboseLog, ref vverboseLog, ref copyAll, ref markedFilesOnly, ref processParallel);
            Log.LogDebug = debugLog;
            Log.LogVerbose = verboseLog;
            Log.LogVeryVerbose = vverboseLog;

            //parse input and output paths
            string input = null,
                output = null;
            ParseStrings(args, ref input, ref output);

            //parse active flavors
            List<string> activeFlavors = ParseActiveFlavors(args);

            //parse file filters
            List<string> filters = ParseFileFilters(args);
            if (filters.Count <= 0)
            {
                //default to all files
                filters.Add("*.*");
            }

            //check input is OK
            if (string.IsNullOrWhiteSpace(input)
               || string.IsNullOrWhiteSpace(output)
               || activeFlavors.Count <= 0)
            {
                Log.e("invalid arguments (input, output empty or no active flavors! exit");
                return;
            }

            //log processing details
            StringBuilder flavorsStr = new StringBuilder();
            foreach (string flavor in activeFlavors)
            {
                flavorsStr.Append(flavor).Append(", ");
            }
            StringBuilder filtersStr = new StringBuilder();
            foreach (string filter in filters)
            {
                filtersStr.Append(filter).Append(", ");
            }
            Log.i($"input: {input}, output: {output}");
            Log.i($"filters: {filtersStr.ToString()}");
            Log.i($"active flavors: {flavorsStr.ToString()}");

            //start processing
            CallChef(input, output, filters, activeFlavors, copyAll, markedFilesOnly, processParallel);
        }

        /// <summary>
        /// call the chef to stir the project up ;)
        /// </summary>
        /// <param name="inputDir">the project input directory path</param>
        /// <param name="outputDir">the project mirror / output direcotry path</param>
        /// <param name="fileFilters">filters to filter files to process by</param>
        /// <param name="activeFlavors">list of active flavors</param>
        /// <param name="copyAll">should all files be copied, no matter if they were changed OR not?</param>
        /// <param name="markedFilesOnly">should only files marked as flavored be processed</param>
        static void CallChef(string inputDir, string outputDir, List<string> fileFilters, List<string> activeFlavors, bool copyAll, bool markedFilesOnly, bool parallel)
        {
            //setup filter function
            FileFilterWrapper filter = new FileFilterWrapper()
            {
                MatchesFilterFunc = (FileInfo file) =>
                {
                    //check if file should be flavored (matches filters)
                    bool matchFilter = false;
                    foreach (string filter in fileFilters)
                    {
                        //convert filter into regex
                        string filterRegex = "^" + Regex.Escape(filter).Replace("\\*", ".*").Replace("\\?", ".") + "$";

                        //check filename with regex
                        if (Regex.IsMatch(file.Name, filterRegex))
                        {
                            matchFilter = true;
                            break;
                        }
                    }

                    //check first line for marking if enabled
                    if (matchFilter && markedFilesOnly)
                    {
                        //get first line
                        string firstLn = File.ReadLines(file.FullName).FirstOrDefault();

                        //check first line contains "flavored" marking
                        //include all files that are marked and matched by the processing filter (flavor may have changed)
                        return !string.IsNullOrWhiteSpace(firstLn) && firstLn.Contains("flavored", StringComparison.OrdinalIgnoreCase);
                    }

                    return matchFilter;
                }
            };

            //check input exists
            DirectoryInfo input = new DirectoryInfo(inputDir);
            if (!input.Exists)
            {
                Log.e($"project directory {inputDir} does not exist!");
                return;
            }

            //create output if needed
            DirectoryInfo output = new DirectoryInfo(outputDir);
            if (!output.Exists)
            {
                output.Create();
                output.Refresh();
            }

            //create chef and start cooking :D
            //use defaults except for filter
            Chef chef = new Chef()
            {
                FlavorFilter = filter
            };
            chef.FlavorProject(input, output, activeFlavors, parallel);
        }

        /// <summary>
        /// show the help page
        /// </summary>
        static void ShowHelp()
        {
            Console.WriteLine(@"
Commandline:
-input    / -i=<DIR>
    input directory
-output   / -o=<DIR>
    output (mirror) directory
    contents will be overwritten

-flavor   / -f=<NAME>
    flavors to apply
    multiple flavors are allowed
-filter    / -ff=<FILTER>
    filter for files to process
    files not matching the filter will only be copied
    multiple filters are allowed
-marked   / -m
    only process files that are marked
    speeds up processing
-all      / -a
    always copy all files, even if unchanged
-parallel / -p
    process files in parallel

-debug    / -d
    enable debug logs
-verbose  / -v
    enable verbose logging
-vverbose / -vv
    enable VERY verbose logging

ex:
$ SmaliChef -i=./src -o=./src-flavored -m -p -f=themeblack -ff=*.smali -ff=*.xml


Expressions:
#[flavor]name=""content"";name=""content"";name=""content"";#[/flavor]
ex:
< TextView name = ""#[flavor]foo=""bar"";test=""string"";#[/flavor]"" />
    with ""-f=foo"" results in
< TextView name = ""bar"" />

when running with - marked, mark flavored files by adding ""flavored"" to the first line in the file
ex:
<? xml version = ""1.0"" ?>< !--flavored-- >
    or
.class Lfoo;#flavored
");
        }

        /// <summary>
        /// parse active flavors from command line
        /// </summary>
        /// <param name="args">command line to parse</param>
        /// <returns>list of all parsed active flavors</returns>
        static List<string> ParseActiveFlavors(string[] args)
        {
            List<string> activeFlavors = new List<string>();
            foreach (string arg in args)
            {
                //split arg on =
                string[] splits = arg.Split('=');
                if (splits.Length != 2
                    || (!splits[0].Equals("-flavor", StringComparison.OrdinalIgnoreCase)
                    && !splits[0].Equals("-f", StringComparison.OrdinalIgnoreCase))) continue;

                //add flavor to list if not already on
                string flavor = splits[1].ToLower();
                if (!activeFlavors.Contains(flavor))
                {
                    activeFlavors.Add(flavor);
                }
            }

            return activeFlavors;
        }

        /// <summary>
        /// parse file filter list from command line
        /// </summary>
        /// <param name="args">command line to parse</param>
        /// <returns>list of all file filters parsed</returns>
        static List<string> ParseFileFilters(string[] args)
        {
            List<string> filters = new List<string>();
            foreach (string arg in args)
            {
                //split arg on =
                string[] splits = arg.Split('=');
                if (splits.Length != 2
                    || (!splits[0].Equals("-filter", StringComparison.OrdinalIgnoreCase)
                    && !splits[0].Equals("-ff", StringComparison.OrdinalIgnoreCase))) continue;

                //add filter to list if not already on
                string filter = splits[1].ToLower();
                if (!filters.Contains(filter))
                {
                    filters.Add(filter);
                }
            }

            return filters;
        }

        /// <summary>
        /// parse the input and output paths from command line
        /// </summary>
        /// <param name="args">command line args to parse</param>
        /// <param name="inputPath">the input path</param>
        /// <param name="outputPath">the output path</param>
        /// <param name="fileFilter">file filter to use for processing files</param>
        static void ParseStrings(string[] args, ref string inputPath, ref string outputPath)
        {
            foreach (string arg in args)
            {
                //split arg on =
                string[] splits = arg.Split('=');
                if (splits.Length != 2) continue;

                if (splits[0].Equals("-input", StringComparison.OrdinalIgnoreCase)
                    || splits[0].Equals("-i", StringComparison.OrdinalIgnoreCase))
                {
                    inputPath = splits[1];
                }

                if (splits[0].Equals("-output", StringComparison.OrdinalIgnoreCase)
                    || splits[0].Equals("-o", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = splits[1];
                }
            }
        }

        /// <summary>
        /// parse logging related flags in command line
        /// </summary>
        /// <param name="args">command line args</param>
        /// <param name="debugLogging">is -debug flag set?</param>
        /// <param name="verboseLogging">is -verbose flag set?</param>
        static void ParseFlags(string[] args, ref bool debugLogging, ref bool verboseLogging, ref bool veryVerboseLogging, ref bool copyAll, ref bool marked, ref bool parallelProcessing)
        {
            debugLogging = args.ContainsIgnoreCase("-debug") || args.ContainsIgnoreCase("-d");
            verboseLogging = args.ContainsIgnoreCase("-verbose") || args.ContainsIgnoreCase("-v");
            veryVerboseLogging = args.ContainsIgnoreCase("-vverbose") || args.ContainsIgnoreCase("-vv");
            copyAll = args.ContainsIgnoreCase("-all") || args.ContainsIgnoreCase("-a");
            marked = args.ContainsIgnoreCase("-marked") || args.ContainsIgnoreCase("-m");
            parallelProcessing = args.ContainsIgnoreCase("-parallel") || args.ContainsIgnoreCase("-p");
        }
    }
}
