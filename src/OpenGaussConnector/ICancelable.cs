using System;
using System.Threading.Tasks;

namespace OpenGaussConnector
{
    interface ICancelable : IDisposable, IAsyncDisposable
    {
        void Cancel();

        Task CancelAsync();
    }
}
