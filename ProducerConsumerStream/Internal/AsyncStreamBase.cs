using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nito.ProducerConsumerStream.Internal
{
    public abstract class AsyncStreamBase: Stream
    {
        protected abstract Task DoFlushAsync(CancellationToken cancellationToken, bool sync);

        public override Task FlushAsync(CancellationToken cancellationToken) => DoFlushAsync(cancellationToken, sync: false);

        public override void Flush() => DoFlushAsync(CancellationToken.None, sync: true).GetAwaiter().GetResult();

        protected abstract Task<int> DoReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            DoReadAsync(buffer, offset, count, cancellationToken, sync: false);

        public override int Read(byte[] buffer, int offset, int count) =>
            DoReadAsync(buffer, offset, count, CancellationToken.None, sync: true).GetAwaiter().GetResult();

        protected abstract Task DoWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            DoWriteAsync(buffer, offset, count, cancellationToken, sync: false);

        public override void Write(byte[] buffer, int offset, int count) =>
            DoWriteAsync(buffer, offset, count, CancellationToken.None, sync: true).GetAwaiter().GetResult();
    }
}
