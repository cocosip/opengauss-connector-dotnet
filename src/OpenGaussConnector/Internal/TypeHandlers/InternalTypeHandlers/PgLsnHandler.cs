using System.Diagnostics;
using OpenGaussConnector.BackendMessages;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;
using OpenGaussConnector.Types;

namespace OpenGaussConnector.Internal.TypeHandlers.InternalTypeHandlers
{
    partial class PgLsnHandler : OpenGaussSimpleTypeHandler<OpenGaussLogSequenceNumber>
    {
        public PgLsnHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        public override OpenGaussLogSequenceNumber Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            Debug.Assert(len == 8);
            return new OpenGaussLogSequenceNumber(buf.ReadUInt64());
        }

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(OpenGaussLogSequenceNumber value, OpenGaussParameter? parameter) => 8;

        public override void Write(OpenGaussLogSequenceNumber value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteUInt64((ulong)value);

        #endregion Write
    }
}
