using System;
using OpenGaussConnector.Internal;
using OpenGaussConnector.Internal.TypeHandling;

namespace OpenGaussConnector.TypeMapping
{
    class BuiltInTypeHandlerResolverFactory : TypeHandlerResolverFactory
    {
        public override TypeHandlerResolver Create(PhysicalConnector connector)
            => new BuiltInTypeHandlerResolver(connector);

        public override string? GetDataTypeNameByClrType(Type clrType)
            => BuiltInTypeHandlerResolver.ClrTypeToDataTypeName(clrType);

        public override string? GetDataTypeNameByValueDependentValue(object value)
            => BuiltInTypeHandlerResolver.ValueDependentValueToDataTypeName(value);

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => BuiltInTypeHandlerResolver.DoGetMappingByDataTypeName(dataTypeName);
    }
}
