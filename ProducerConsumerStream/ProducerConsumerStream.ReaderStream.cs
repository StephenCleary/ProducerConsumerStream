using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.ProducerConsumerStream.Internal;

namespace Nito.ProducerConsumerStream
{
    partial class ProducerConsumerStream
    {
        private sealed class ReaderStream : AsyncStreamBase
        {
            private readonly ProducerConsumerStream _producerConsumerStream;

            public ReaderStream(ProducerConsumerStream producerConsumerStream)
            {
                _producerConsumerStream = producerConsumerStream;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => _producerConsumerStream.ReaderPosition;
                set => throw new NotSupportedException();
            }

            protected override Task DoFlushAsync(CancellationToken cancellationToken, bool sync) => Task.FromException(new NotSupportedException());

            protected override Task<int> DoReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync) =>
                _producerConsumerStream.ReadAsync(buffer, offset, count, cancellationToken, sync);

            protected override Task DoWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync) =>
                Task.FromException(new NotSupportedException());
        }
    }
}
