using System;
using Xunit;

namespace CoreUtilitiesPack.Test
{
    public class SqliteAccessorTest
    {
        [Fact]
        public void Test1()
        {
            var sa = new SqliteAccessor();
            var sql = "select sqlite_version()";
            var result = sa.ExecuteQuery(sql);
            result.Count.IsNot(0);
        }
    }
}
