using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OpenGaussConnector.Internal;
using OpenGaussConnector.PostgresTypes;
using OpenGaussConnector.Util;
using OpenGaussConnector.Types;

namespace OpenGaussConnector
{
    class PostgresMinimalDatabaseInfoFactory : IOpenGaussDatabaseInfoFactory
    {
        public Task<OpenGaussDatabaseInfo?> Load(PhysicalConnector conn, OpenGaussTimeout timeout, bool async)
            => Task.FromResult(
               conn.Settings.ServerCompatibilityMode == ServerCompatibilityMode.NoTypeLoading
                    ? (OpenGaussDatabaseInfo)new PostgresMinimalDatabaseInfo(conn)
                    : null
            );
    }

    class PostgresMinimalDatabaseInfo : PostgresDatabaseInfo
    {
        static readonly PostgresBaseType[] Types = typeof(OpenGaussDbType).GetFields()
            .Select(f => f.GetCustomAttribute<BuiltInPostgresType>())
            .OfType<BuiltInPostgresType>()
            .Select(a => new PostgresBaseType("pg_catalog", a.Name, a.OID))
            .ToArray();

        protected override IEnumerable<PostgresType> GetTypes() => Types;

        internal PostgresMinimalDatabaseInfo(PhysicalConnector conn)
            : base(conn)
        {
            HasIntegerDateTimes = !conn.PostgresParameters.TryGetValue("integer_datetimes", out var intDateTimes) ||
                                  intDateTimes == "on";
        }
    }
}
