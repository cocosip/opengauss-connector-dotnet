using System.Diagnostics;
using OpenGaussConnector.BackendMessages;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;
using OpenGaussConnector.Types;

namespace OpenGaussConnector.Internal.TypeHandlers.InternalTypeHandlers
{
    partial class TidHandler : OpenGaussSimpleTypeHandler<OpenGaussTid>
    {
        public TidHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        public override OpenGaussTid Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            Debug.Assert(len == 6);

            var blockNumber = buf.ReadUInt32();
            var offsetNumber = buf.ReadUInt16();

            return new OpenGaussTid(blockNumber, offsetNumber);
        }

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(OpenGaussTid value, OpenGaussParameter? parameter)
            => 6;

        public override void Write(OpenGaussTid value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteUInt32(value.BlockNumber);
            buf.WriteUInt16(value.OffsetNumber);
        }

        #endregion Write
    }
}
