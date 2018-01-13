using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Nito.ProducerConsumerStream
{
    /// <summary>
    /// An in-memory stream that can be simultaneously read and written to. The <see cref="Writer"/> (producer) and <see cref="Reader"/> (consumer) APIs are exposed as two different stream instances.
    /// </summary>
    public sealed partial class ProducerConsumerStream
    {
        private readonly int _maxBytes;
        private readonly LinkedList<byte[]> _data;

        /// <summary>
        /// The total number of bytes available for reading from the data.
        /// </summary>
        private int _currentBytes;

        /// <summary>
        /// Number of bytes in the header node of <see cref="_data"/> that have already been read.
        /// </summary>
        private int _headDataBytesRead;

        /// <summary>
        /// The writing stream has been closed; no further writes will be done.
        /// </summary>
        private bool _completed;

        private readonly AsyncLock _mutex;
        private readonly AsyncConditionVariable _notFullOrCompleted;
        private readonly AsyncConditionVariable _notEmptyOrCompleted;

        /// <summary>
        /// Constructs a new in-memory stream.
        /// </summary>
        /// <param name="maxBytes">The maximum number of bytes to hold available for reading. Note that due to buffers, the memory usage of this type may be higher than this parameter.</param>
        public ProducerConsumerStream(int maxBytes = int.MaxValue)
        {
            _maxBytes = maxBytes;
            _data = new LinkedList<byte[]>();
            _mutex = new AsyncLock();
            _notFullOrCompleted = new AsyncConditionVariable(_mutex);
            _notEmptyOrCompleted = new AsyncConditionVariable(_mutex);
            Reader = new ReaderStream(this);
            Writer = new WriterStream(this);
        }

        /// <summary>
        /// The read-only side of this producer/consumer stream.
        /// </summary>
        public Stream Reader { get; }

        /// <summary>
        /// The write-only side of this producer/consumer stream. This stream may be disposed to signal that the producer has completed writing.
        /// </summary>
        public Stream Writer { get; }

        private bool Empty => _currentBytes == 0;
        private bool Full => _currentBytes == _maxBytes;
        private int AvailableToWrite => _maxBytes - _currentBytes;
        private int AvailableToRead => _data.First.Value.Length - _headDataBytesRead;
        private long WriterPosition { get; set; }
        private long ReaderPosition { get; set; }

        private void CompleteWriting()
        {
            using (_mutex.Lock())
            {
                _completed = true;
                _notFullOrCompleted.NotifyAll();
                _notEmptyOrCompleted.NotifyAll();
            }
        }

        private async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync)
        {
            using (sync ? _mutex.Lock(cancellationToken) : await _mutex.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                while (count != 0)
                {
                    while (Full && !_completed)
                    {
                        if (sync)
                            _notFullOrCompleted.Wait(cancellationToken);
                        else
                            await _notFullOrCompleted.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    if (_completed)
                        throw new OperationCanceledException("Stream has been closed for writing.");
                    cancellationToken.ThrowIfCancellationRequested();

                    // Copy the data
                    var bytesToCopy = Math.Min(count, AvailableToWrite);
                    var data = new byte[bytesToCopy];
                    Array.Copy(buffer, offset, data, 0, bytesToCopy);

                    // Add it to the stream
                    _data.AddLast(data);
                    _currentBytes += bytesToCopy;
                    WriterPosition += bytesToCopy;

                    // Adjust status of current operation
                    offset += bytesToCopy;
                    count -= bytesToCopy;

                    _notEmptyOrCompleted.Notify();
                }
            }
        }

        private async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, bool sync)
        {
            using (sync ? _mutex.Lock(cancellationToken) : await _mutex.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                while (Empty && !_completed)
                {
                    if (sync)
                        _notEmptyOrCompleted.Wait(cancellationToken);
                    else
                        await _notEmptyOrCompleted.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (AvailableToRead == 0)
                    return 0;

                // Copy the data from the stream
                var bytesToCopy = Math.Min(count, AvailableToRead);
                Array.Copy(_data.First.Value, _headDataBytesRead, buffer, offset, bytesToCopy);

                // Remove those bytes from the stream
                if (bytesToCopy == AvailableToRead)
                {
                    _data.RemoveFirst();
                    _headDataBytesRead = 0;
                }
                else
                {
                    _headDataBytesRead += bytesToCopy;
                }
                _currentBytes -= bytesToCopy;
                ReaderPosition += bytesToCopy;

                _notFullOrCompleted.Notify();
                return bytesToCopy;
            }
        }
    }
}
