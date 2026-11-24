using System;
using OpenGaussConnector.Internal;
using OpenGaussConnector.Internal.TypeHandling;

namespace OpenGaussConnector.NodaTime.Internal
{
    public class NodaTimeTypeHandlerResolverFactory : TypeHandlerResolverFactory
    {
        public override TypeHandlerResolver Create(PhysicalConnector connector)
            => new NodaTimeTypeHandlerResolver(connector);

        public override string? GetDataTypeNameByClrType(Type type)
            => NodaTimeTypeHandlerResolver.ClrTypeToDataTypeName(type);

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => NodaTimeTypeHandlerResolver.DoGetMappingByDataTypeName(dataTypeName);
    }
}
