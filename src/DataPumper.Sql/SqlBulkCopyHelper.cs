using System.Data.SqlClient;
using System.Reflection;

namespace DataPumper.Sql
{
    /// <summary>
    /// Helper class to process the SqlBulkCopy class
    /// </summary>
    public static class SqlBulkCopyHelper
    {
        private static FieldInfo _rowsCopiedField;

        /// <summary>
        /// Gets the rows copied from the specified SqlBulkCopy object
        /// </summary>
        /// <param name="bulkCopy">The bulk copy.</param>
        /// <returns></returns>
        public static int GetRowsCopied(this SqlBulkCopy bulkCopy)
        {
            if (_rowsCopiedField == null)
            {
                _rowsCopiedField = typeof(SqlBulkCopy).GetField("_rowsCopied", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            }

            return (int)_rowsCopiedField.GetValue(bulkCopy);
        }
    }
}
