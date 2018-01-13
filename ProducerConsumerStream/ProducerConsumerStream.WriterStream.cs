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
        private sealed class WriterStream : AsyncStreamBase
        {
            private readonly ProducerConsumerStream _producerConsumerStream;

            public WriterStream(ProducerConsumerStream producerConsumerStream)
            {
                _producerConsumerStream = producerConsumerStream;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => _producerConsumerStream.WriterPosition;
                set => throw new NotSupportedException();
            }

            protected override Task DoFlushAsync(CancellationToken cancellationToken, bool sync) => Task.CompletedTask;

            protected override Task<int> DoReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync) =>
                Task.FromException<int>(new NotSupportedException());

            protected override Task DoWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync) =>
                _producerConsumerStream.WriteAsync(buffer, offset, count, cancellationToken, sync);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _producerConsumerStream.CompleteWriting();
                base.Dispose(disposing);
            }
        }
    }
}
