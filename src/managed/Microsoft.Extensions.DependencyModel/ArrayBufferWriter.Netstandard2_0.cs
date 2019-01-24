// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyModel
{
    internal class ArrayBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _rentedBuffer;
        private int _written;

        private const int MinimumBufferSize = 256;

        public ArrayBufferWriter(int initialCapacity = MinimumBufferSize)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException(nameof(initialCapacity));

            _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _written = 0;
        }

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (_rentedBuffer == null)
                throw new ObjectDisposedException(nameof(ArrayBufferWriter));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            await stream.WriteAsync(_rentedBuffer, 0, _written, cancellationToken).ConfigureAwait(false);

            _rentedBuffer.AsSpan(0, _written).Clear();
            _written = 0;
        }

        internal void CopyTo(Stream stream)
        {
            if (_rentedBuffer == null)
                throw new ObjectDisposedException(nameof(ArrayBufferWriter));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            stream.Write(_rentedBuffer, 0, _written);

            _rentedBuffer.AsSpan(0, _written).Clear();
            _written = 0;
        }

        public void Advance(int count)
        {
            if (_rentedBuffer == null)
                throw new ObjectDisposedException(nameof(ArrayBufferWriter));

            if (count < 0)
                throw new ArgumentException(nameof(count));

            if (_written > _rentedBuffer.Length - count)
                throw new InvalidOperationException("Cannot advance past the end of the buffer.");

            _written += count;
        }

        // Returns the rented buffer back to the pool
        public void Dispose()
        {
            if (_rentedBuffer == null)
            {
                return;
            }

            ArrayPool<byte>.Shared.Return(_rentedBuffer, clearArray: true);
            _rentedBuffer = null;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_rentedBuffer == null)
                throw new ObjectDisposedException(nameof(ArrayBufferWriter));

            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            CheckAndResizeBuffer(sizeHint);
            return _rentedBuffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_rentedBuffer == null)
                throw new ObjectDisposedException(nameof(ArrayBufferWriter));

            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            CheckAndResizeBuffer(sizeHint);
            return _rentedBuffer.AsSpan(_written);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            Debug.Assert(sizeHint >= 0);

            if (sizeHint == 0)
            {
                sizeHint = _rentedBuffer.Length == 0 ? MinimumBufferSize : checked(_rentedBuffer.Length * 2);

                Debug.Assert(sizeHint > _rentedBuffer.Length);
            }

            if (sizeHint > _rentedBuffer.Length)
            {
                byte[] oldBuffer = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);

                Debug.Assert(oldBuffer.Length >= _written);
                Debug.Assert(_rentedBuffer.Length >= _written);

                oldBuffer.AsSpan(0, _written).CopyTo(_rentedBuffer);
                ArrayPool<byte>.Shared.Return(oldBuffer, clearArray: true);
            }

            Debug.Assert(_rentedBuffer.Length > 0);
        }
    }
}
