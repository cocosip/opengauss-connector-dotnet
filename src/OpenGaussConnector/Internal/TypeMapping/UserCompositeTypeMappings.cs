using System;
using OpenGaussConnector.Internal.TypeHandlers.CompositeHandlers;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;

namespace OpenGaussConnector.Internal.TypeMapping
{
    public interface IUserCompositeTypeMapping : IUserTypeMapping
    {
        IOpenGaussNameTranslator NameTranslator { get; }
    }

    class UserCompositeTypeMapping<T> : IUserCompositeTypeMapping
    {
        public string PgTypeName { get; }
        public Type ClrType => typeof(T);
        public IOpenGaussNameTranslator NameTranslator { get; }

        public UserCompositeTypeMapping(string pgTypeName, IOpenGaussNameTranslator nameTranslator)
            => (PgTypeName, NameTranslator) = (pgTypeName, nameTranslator);

        public OpenGaussTypeHandler CreateHandler(PostgresType pgType, PhysicalConnector connector)
            => new CompositeHandler<T>((PostgresCompositeType)pgType, connector.TypeMapper, NameTranslator);
    }
}
