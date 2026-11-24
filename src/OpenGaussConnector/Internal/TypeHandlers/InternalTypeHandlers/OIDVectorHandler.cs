using System;
using OpenGaussConnector.Internal.TypeHandlers.NumericHandlers;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;
using OpenGaussConnector.TypeMapping;
using OpenGaussConnector.Types;

namespace OpenGaussConnector.Internal.TypeHandlers.InternalTypeHandlers
{
    /// <summary>
    /// An OIDVector is simply a regular array of uints, with the sole exception that its lower bound must
    /// be 0 (we send 1 for regular arrays).
    /// </summary>
    class OIDVectorHandler : ArrayHandler<uint>
    {
        public OIDVectorHandler(PostgresType oidvectorType, PostgresType oidType)
            : base(oidvectorType, new UInt32Handler(oidType), ArrayNullabilityMode.Never, 0) { }

        public override OpenGaussTypeHandler CreateArrayHandler(PostgresArrayType pgArrayType, ArrayNullabilityMode arrayNullabilityMode)
            => new ArrayHandler<ArrayHandler<uint>>(pgArrayType, this, arrayNullabilityMode);
    }
}
