using System;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;

namespace OpenGaussConnector.Internal.TypeMapping
{
    public interface IUserTypeMapping
    {
        public string PgTypeName { get; }
        public Type ClrType { get; }

        public OpenGaussTypeHandler CreateHandler(PostgresType pgType, PhysicalConnector connector);
    }
}
