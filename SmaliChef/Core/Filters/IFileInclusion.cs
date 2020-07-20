using System;
using System.IO;

namespace SmaliChef.Core.Filters
{
    /// <summary>
    /// interface for file inclusions
    /// </summary>
    public interface IFileInclusion
    {
        /// <summary>
        /// should the input file be processed to the output file?
        /// </summary>
        /// <param name="inputFile">the file to process</param>
        /// <param name="outputFile">where the file will be written to after processing finishes</param>
        /// <param name="matchesFileFilter">does the input file match the previous file filter?</param>
        /// <returns>should include?</returns>
        bool ShouldInclude(FileInfo inputFile, FileInfo outputFile, bool matchesFileFilter);

        /// <summary>
        /// called after the input file was processed to the output file
        /// </summary>
        /// <param name="inputFile">the file processed</param>
        /// <param name="outputFile">the processed file</param>
        void PostProcessed(FileInfo inputFile, FileInfo outputFile);
    }

    /// <summary>
    /// default implementation of IFileInclusion, includes all files
    /// </summary>
    public class DefaultFileInclusion : IFileInclusion
    {
        /// <summary>
        /// should the input file be processed to the output file?
        /// </summary>
        /// <param name="inputFile">the file to process</param>
        /// <param name="outputFile">where the file will be written to after processing finishes</param>
        /// <returns>should include?</returns>
        public bool ShouldInclude(FileInfo inputFile, FileInfo outputFile, bool matchesFileFilter)
        {
            return true;
        }

        /// <summary>
        /// called after the input file was processed to the output file
        /// </summary>
        /// <param name="inputFile">the file processed</param>
        /// <param name="outputFile">the processed file</param>
        public void PostProcessed(FileInfo inputFile, FileInfo outputFile) { }
    }

    /// <summary>
    /// wraps IFileInclusion to delegates
    /// </summary>
    public class FileInclusionWrapper : IFileInclusion
    {
        /// <summary>
        /// Wrapper for IFileInclusion#ShouldInclude()
        /// </summary>
        public Func<FileInfo, FileInfo, bool, bool> ShouldIncludeFunc { get; set; }

        /// <summary>
        /// Wrapper for IFileInclusion#PostProcessedFunc()
        /// </summary>
        public Action<FileInfo, FileInfo> PostProcessedFunc { get; set; }

        /// <summary>
        /// should the input file be processed to the output file?
        /// </summary>
        /// <param name="inputFile">the file to process</param>
        /// <param name="outputFile">where the file will be written to after processing finishes</param>
        /// <returns>should include?</returns>
        public bool ShouldInclude(FileInfo inputFile, FileInfo outputFile, bool matchesFileFilter)
        {
            if (ShouldIncludeFunc == null) return false;
            return ShouldIncludeFunc.Invoke(inputFile, outputFile, matchesFileFilter);
        }

        /// <summary>
        /// called after the input file was processed to the output file
        /// </summary>
        /// <param name="inputFile">the file processed</param>
        /// <param name="outputFile">the processed file</param>
        public void PostProcessed(FileInfo inputFile, FileInfo outputFile)
        {
            PostProcessedFunc?.Invoke(inputFile, outputFile);
        }
    }
}
