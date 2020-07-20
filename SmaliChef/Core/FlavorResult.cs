namespace SmaliChef.Core
{
    /// <summary>
    /// results of FlavorFile function
    /// </summary>
    public enum FlavorResult
    {
        /// <summary>
        /// the file was processed and flavored, and replaced an possibly existing output file
        /// </summary>
        FlavoredReplace,

        /// <summary>
        /// the file was processed and flavored, but matched the existing output file, so was not replaced
        /// </summary>
        FlavoredSkipped,

        /// <summary>
        /// the file was copied as it did match the inclusion filter, but not the flavoring filter
        /// </summary>
        Copied,

        /// <summary>
        /// the file did not match the inclusion filter
        /// </summary>
        Skipped
    }
}
