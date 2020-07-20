using System;
using System.IO;

namespace SmaliChef.Core.Filters
{
    /// <summary>
    /// interface for file filtering
    /// </summary>
    public interface IFileFilter
    {
        /// <summary>
        /// does the file fit the filter
        /// </summary>
        /// <param name="file">the file to check</param>
        /// <returns>fits filter?</returns>
        bool MatchesFilter(FileInfo file);
    }

    /// <summary>
    /// default implementation of IFileFilter, applies no filter
    /// </summary>
    public class DefaultFileFilter : IFileFilter
    {
        /// <summary>
        /// does the file fit the filter
        /// </summary>
        /// <param name="file">the file to check</param>
        /// <returns>fits filter?</returns>
        public bool MatchesFilter(FileInfo file)
        {
            return true;
        }
    }

    /// <summary>
    /// wraps IFileFilter to a delegate
    /// </summary>
    public class FileFilterWrapper : IFileFilter
    {
        /// <summary>
        /// wrapper for IFileFilter#MatchesFilter()
        /// </summary>
        public Func<FileInfo, bool> MatchesFilterFunc { get; set; }

        /// <summary>
        /// does the file fit the filter
        /// </summary>
        /// <param name="file">the file to check</param>
        /// <returns>fits filter?</returns>
        public bool MatchesFilter(FileInfo file)
        {
            if (MatchesFilterFunc == null) return false;
            return MatchesFilterFunc.Invoke(file);
        }
    }
}
