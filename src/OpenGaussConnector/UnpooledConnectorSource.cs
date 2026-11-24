using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using OpenGaussConnector.Internal;
using OpenGaussConnector.Util;

namespace OpenGaussConnector
{
    sealed class UnpooledConnectorSource : ConnectorSource
    {
        public UnpooledConnectorSource(OpenGaussConnectionStringBuilder settings, string connString)
            : base(settings, connString)
        {
        }

        volatile int _numConnectors;

        internal override (int Total, int Idle, int Busy) Statistics => (_numConnectors, 0, _numConnectors);

        internal override bool OwnsConnectors => true;

        internal override async ValueTask<PhysicalConnector> Get(
            OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken)
        {
            var connector = new PhysicalConnector(this, conn);
            await connector.Open(timeout, async, cancellationToken);
            Interlocked.Increment(ref _numConnectors);
            return connector;
        }

        internal override bool TryGetIdleConnector([NotNullWhen(true)] out PhysicalConnector? connector)
        {
            connector = null;
            return false;
        }

        internal override ValueTask<PhysicalConnector?> OpenNewConnector(
            OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken)
            => new((PhysicalConnector?)null);

        internal override void Return(PhysicalConnector connector)
        {
            Interlocked.Decrement(ref _numConnectors);
            connector.Close();
        }

        internal override void Clear() {}

        internal override bool TryRentEnlistedPending(Transaction transaction, OpenGaussConnection connection,
            [NotNullWhen(true)] out PhysicalConnector? connector)
        {
            connector = null;
            return false;
        }

        internal override bool TryRemovePendingEnlistedConnector(PhysicalConnector connector, Transaction transaction) => false;
    }
}
