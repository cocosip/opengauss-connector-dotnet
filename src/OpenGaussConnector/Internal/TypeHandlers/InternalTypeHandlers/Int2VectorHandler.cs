using System;
using OpenGaussConnector.Internal.TypeHandlers.NumericHandlers;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;
using OpenGaussConnector.TypeMapping;
using OpenGaussConnector.Types;

namespace OpenGaussConnector.Internal.TypeHandlers.InternalTypeHandlers
{
    /// <summary>
    /// An int2vector is simply a regular array of shorts, with the sole exception that its lower bound must
    /// be 0 (we send 1 for regular arrays).
    /// </summary>
    class Int2VectorHandler : ArrayHandler<short>
    {
        public Int2VectorHandler(PostgresType arrayPostgresType, PostgresType postgresShortType)
            : base(arrayPostgresType, new Int16Handler(postgresShortType), ArrayNullabilityMode.Never, 0) { }

        public override OpenGaussTypeHandler CreateArrayHandler(PostgresArrayType pgArrayType, ArrayNullabilityMode arrayNullabilityMode)
            => new ArrayHandler<ArrayHandler<short>>(pgArrayType, this, arrayNullabilityMode);
    }
}
