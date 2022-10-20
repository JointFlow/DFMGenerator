using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Slb.Ocean.Core;
using Slb.Ocean.Petrel.Data;
using Slb.Ocean.Petrel.Data.Persistence;


namespace DFNGenerator_Ocean
{
    class DFNGeneratorDataSourceFactory : DataSourceFactory
    {
        public static string DataSourceId = "DFNGenerator_Ocean.DFNGeneratorDataSourceFactory";
        public override Slb.Ocean.Core.IDataSource GetDataSource()
        {
            StructuredArchiveDataSource dataSource = new StructuredArchiveDataSource(DataSourceId, new[] { typeof(DFNGenerator.Arguments) });
            return dataSource;
        }
        // Helper method
        public static StructuredArchiveDataSource Get(IDataSourceManager dsm)
        {
            return dsm.GetSource(DataSourceId) as StructuredArchiveDataSource;
        }
    }
}
