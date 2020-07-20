using SmaliChef.Core.Filters;
using SmaliChef.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace SmaliChef.Core
{
    /// <summary>
    /// handles flavoring of whole projects
    /// </summary>
    public class Chef
    {
        /// <summary>
        /// Filter for files to process in the project. applies to both flavoring and copying.
        /// </summary>
        public IFileInclusion IncludeFilter { get; set; } = new DefaultFileInclusion();

        /// <summary>
        /// Filter for files to flavor. files that dont fit the filter will just be copied.
        /// </summary>
        public IFileFilter FlavorFilter { get; set; } = new DefaultFileFilter();

        /// <summary>
        /// Comparator to compare files with another
        /// </summary>
        public IFileComparator FileComparator { get; set; } = new DefaultFileComparator();

        /// <summary>
        /// Seasoner this Chef uses
        /// </summary>
        Seasoner seasoner = new Seasoner();

        /// <summary>
        /// Flavor a project directory
        /// </summary>
        /// <param name="inputDir">the directory to flavor</param>
        /// <param name="outputDir">where the project directory is mirrored to, flavored</param>
        /// <param name="activeFlavors">a list of all active flavors</param>
        /// <param name="parallelProcessing">should files be processed in parallel?</param>
        public void FlavorProject(DirectoryInfo inputDir, DirectoryInfo outputDir, List<string> activeFlavors, bool parallelProcessing = false)
        {
            //counters for flavoring results
            int flavoredChanged = 0,
                flavoredUnchanged = 0,
                copied = 0,
                skipped = 0;

            //setup function for each file (~= loop body)
            Action<FileInfo> processAction = (FileInfo input) =>
            {
                using (Log.AsyncLogSession logSession = Log.StartAsync())
                {
                    //set tag of session
                    logSession.PushTag(input.Name);

                    //create output fileinfo
                    FileInfo output = new FileInfo(Path.Combine(outputDir.FullName, Path.GetRelativePath(inputDir.FullName, input.FullName)));

                    //flavor the file, count results
                    switch (FlavorFile(input, output, activeFlavors, logSession))
                    {
                        case FlavorResult.FlavoredReplace:
                            flavoredChanged++;
                            break;
                        case FlavorResult.FlavoredSkipped:
                            flavoredUnchanged++;
                            break;
                        case FlavorResult.Copied:
                            copied++;
                            break;
                        case FlavorResult.Skipped:
                            skipped++;
                            break;
                    }
                }
            };

            //enumerate files
            if (parallelProcessing)
            {
                inputDir.EnumerateAllFilesParallel("*.*", true, processAction);
            }
            else
            {
                inputDir.EnumerateAllFiles("*.*", true, processAction);
            }

            //log results
            Log.i($"Finished processing. {flavoredChanged} flavored files changed, {flavoredUnchanged} unchanged, {copied} files copied and {skipped} files skipped entirely.");
        }

        /// <summary>
        /// flavor a single file
        /// </summary>
        /// <param name="input">input file to flavor</param>
        /// <param name="output">where the file is mirrored to, flavored</param>
        /// <param name="activeFlavors">a list of all active flavors</param>
        /// <param name="logSession">logging session for this file</param>
        public FlavorResult FlavorFile(FileInfo input, FileInfo output, List<string> activeFlavors, Log.AsyncLogSession logSession)
        {
            //check filter first
            bool shouldFlavor = FlavorFilter.MatchesFilter(input);

            //check if file should be included
            if (!IncludeFilter.ShouldInclude(input, output, shouldFlavor))
            {
                logSession?.v("Skipping file: does not match includeFilter!");
                return FlavorResult.Skipped;
            }

            //check if file should be flavored or just copied
            if (shouldFlavor)
            {
                //process into a temp file
                FileInfo temp = new FileInfo(Path.GetTempFileName());
                using (StreamReader inputStream = input.OpenText())
                using (StreamWriter outputStream = temp.CreateText())
                {
                    seasoner.FlavorStream(inputStream, outputStream, activeFlavors, logSession);
                }

                //check if old output and temp file are the same
                if (output.Exists && FileComparator.IsSameFile(output, temp))
                {
                    logSession.v("old processed file matches the new one, not replacing");
                    temp.Delete();
                    return FlavorResult.FlavoredSkipped;
                }
                else
                {
                    //not equal, move temp to output
                    string delTemp = temp.FullName;
                    temp.MoveTo(output.FullName, true);
                    IncludeFilter.PostProcessed(input, output);

                    //delete old temp file
                    if (File.Exists(delTemp)) File.Delete(delTemp);
                    return FlavorResult.FlavoredReplace;
                }
            }
            else
            {
                //check if file in output is the same as in input
                if (output.Exists && FileComparator.IsSameFile(input, output))
                {
                    logSession.v("old copied file matches file in input, skipping");
                    return FlavorResult.Skipped;
                }

                //copy file from input to output
                input.CopyTo(output.FullName, true);
                IncludeFilter.PostProcessed(input, output);
                return FlavorResult.Copied;
            }
        }
    }
}
