using OpenGaussConnector.Internal;

namespace OpenGaussConnector.BackendMessages
{
    class ReadyForQueryMessage : IBackendMessage
    {
        public BackendMessageCode Code => BackendMessageCode.ReadyForQuery;

        internal TransactionStatus TransactionStatusIndicator { get; private set; }

        internal ReadyForQueryMessage Load(OpenGaussReadBuffer buf) {
            TransactionStatusIndicator = (TransactionStatus)buf.ReadByte();
            return this;
        }
    }
}
