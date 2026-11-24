using System;
using System.Diagnostics;
using NodaTime;
using OpenGaussConnector;
using OpenGaussConnector.BackendMessages;
using OpenGaussConnector.Internal;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;
using BclTimestampHandler = OpenGaussConnector.Internal.TypeHandlers.DateTimeHandlers.TimestampHandler;

namespace OpenGaussConnector.NodaTime.Internal
{
    sealed partial class LegacyTimestampHandler : OpenGaussSimpleTypeHandler<Instant>,
        IOpenGaussSimpleTypeHandler<LocalDateTime>, IOpenGaussSimpleTypeHandler<DateTime>, IOpenGaussSimpleTypeHandler<long>
    {
        readonly BclTimestampHandler _bclHandler;

        internal LegacyTimestampHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclTimestampHandler(postgresType);

        #region Read

        public override Instant Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => TimestampTzHandler.ReadInstant(buf);

        LocalDateTime IOpenGaussSimpleTypeHandler<LocalDateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => TimestampHandler.ReadLocalDateTime(buf);

        DateTime IOpenGaussSimpleTypeHandler<DateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read(buf, len, fieldDescription);

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).Read(buf, len, fieldDescription);

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(Instant value, OpenGaussParameter? parameter)
            => 8;

        int IOpenGaussSimpleTypeHandler<LocalDateTime>.ValidateAndGetLength(LocalDateTime value, OpenGaussParameter? parameter)
            => 8;

        public override void Write(Instant value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => TimestampTzHandler.WriteInstant(value, buf);

        void IOpenGaussSimpleTypeHandler<LocalDateTime>.Write(LocalDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => TimestampHandler.WriteLocalDateTime(value, buf);

        int IOpenGaussSimpleTypeHandler<DateTime>.ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_bclHandler).ValidateAndGetLength(value, parameter);

        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTime>.Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_bclHandler).Write(value, buf, parameter);

        void IOpenGaussSimpleTypeHandler<long>.Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).Write(value, buf, parameter);

        #endregion Write
    }
}
