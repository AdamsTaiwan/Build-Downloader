using System.Data;

namespace BuildDownloader
{
    public static class BuildSet
    {
        public static DataSet New()
        {
            DataSet ds = new DataSet("R");
            DataTable dt = new DataTable("B");

            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("id", typeof(string), "", MappingType.Attribute),
                new DataColumn("sessionId", typeof(string), "", MappingType.Attribute),
                new DataColumn("langLocale", typeof(string), "", MappingType.Attribute),
                new DataColumn("sessionCode", typeof(string), "", MappingType.Attribute),
                new DataColumn("title", typeof(string), "", MappingType.Attribute),
                new DataColumn("sortRank", typeof(string), "", MappingType.Attribute),
                new DataColumn("level", typeof(string), "", MappingType.Attribute),
                new DataColumn("sessionTypeId", typeof(string), "", MappingType.Attribute),
                new DataColumn("sessionType", typeof(string), "", MappingType.Attribute),
                new DataColumn("durationInMinutes", typeof(int), "", MappingType.Attribute),
                new DataColumn("lastUpdate", typeof(string), "", MappingType.Attribute),
                new DataColumn("visibleInSessionListing", typeof(bool), "", MappingType.Attribute),
                new DataColumn("slideDeck", typeof(string), "", MappingType.Attribute),
                new DataColumn("downloadVideoLink", typeof(string), "", MappingType.Attribute),
                new DataColumn("captionFileLink", typeof(string), "", MappingType.Attribute),
                new DataColumn("onDemandThumbnail", typeof(string), "", MappingType.Attribute),
                new DataColumn("hasSlides", typeof(bool), "", MappingType.Attribute),
                new DataColumn("hasVideo", typeof(bool), "", MappingType.Attribute),
                new DataColumn("hasChanged", typeof(bool), "", MappingType.Attribute),
                new DataColumn("desciption", typeof(string), "", MappingType.Attribute)
            });
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0]};
            ds.Tables.Add(dt);
            return ds;
        }
    }
}
