using System.Data;

namespace BuildDownloader
{
    public static class FeedList
    {
        public static DataSet New()
        {
            DataSet ds = new DataSet("R");
            DataTable dt = new DataTable("Feed");

            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("name", typeof(string), "", MappingType.Attribute),
                new DataColumn("type", typeof(int), "", MappingType.Attribute),
                new DataColumn("saveto", typeof(string), "", MappingType.Attribute),
                new DataColumn("url", typeof(string), "", MappingType.Attribute),
                new DataColumn("note", typeof(string), "", MappingType.Attribute)
            });
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };
            ds.Tables.Add(dt);
            return ds;
        }
    }
}
