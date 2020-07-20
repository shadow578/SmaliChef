using SmaliChef.Util;
using System;
using System.IO;

namespace SmaliChef.Core.Filters
{
    /// <summary>
    /// interface for file comparisions
    /// </summary>
    public interface IFileComparator
    {
        /// <summary>
        /// are the two files equal
        /// </summary>
        /// <param name="fileA">the first file</param>
        /// <param name="fileB">the second file</param>
        /// <returns>are the files equal?</returns>
        bool IsSameFile(FileInfo fileA, FileInfo fileB);
    }

    /// <summary>
    /// Default implementation of IFileComparator, uses MD5 hash
    /// </summary>
    public class DefaultFileComparator : IFileComparator
    {
        /// <summary>
        /// are the two files equal
        /// </summary>
        /// <param name="fileA">the first file</param>
        /// <param name="fileB">the second file</param>
        /// <returns>are the files equal?</returns>
        public bool IsSameFile(FileInfo fileA, FileInfo fileB)
        {
            return fileA.HasSameHash(fileB);
        }
    }

    /// <summary>
    /// wraps IFileComparator with a delegate
    /// </summary>
    public class FileComparatorWrapper : IFileComparator
    {
        /// <summary>
        /// wrapper for IFileComparator#IsSameFile()
        /// </summary>
        public Func<FileInfo, FileInfo, bool> IsSameFileFunc { get; set; }

        /// <summary>
        /// are the two files equal
        /// </summary>
        /// <param name="fileA">the first file</param>
        /// <param name="fileB">the second file</param>
        /// <returns>are the files equal?</returns>
        public bool IsSameFile(FileInfo fileA, FileInfo fileB)
        {
            if (IsSameFileFunc == null) return false;
            return IsSameFileFunc.Invoke(fileA, fileB);
        }
    }
}
