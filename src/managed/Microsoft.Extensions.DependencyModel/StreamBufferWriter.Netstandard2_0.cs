// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal struct StreamBufferWriter : IBufferWriter<byte>, IDisposable
    {
        Stream _stream;
        byte[] _rentedBuffer;

        private const int MinimumBufferSize = 256;

        public StreamBufferWriter(Stream stream, int bufferSize = MinimumBufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentException(nameof(bufferSize));
            }
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            _rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        }

        public void Advance(int count)
        {
            _stream.Write(_rentedBuffer, 0, count);
            _rentedBuffer.AsSpan(0, count).Clear();
        }

        /// <summary>
        /// Returns rented buffer back to the pool
        /// </summary>
        public void Dispose()
        {
            _rentedBuffer.AsSpan().Clear();
            ArrayPool<byte>.Shared.Return(_rentedBuffer);
            _rentedBuffer = null;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                int newSize = _rentedBuffer.Length == 0 ? MinimumBufferSize : checked (_rentedBuffer.Length * 2);
                byte[] temp = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(newSize);
                temp.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(temp);
            }
            else if (sizeHint > _rentedBuffer.Length)
            {
                var temp = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
                temp.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(temp);
            }
            Debug.Assert(_rentedBuffer.Length > 0);
            return _rentedBuffer;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                int newSize = _rentedBuffer.Length == 0 ? MinimumBufferSize : checked(_rentedBuffer.Length * 2);
                byte[] temp = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(newSize);
                temp.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(temp);
            }
            else if (sizeHint > _rentedBuffer.Length)
            {
                var temp = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
                temp.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(temp);
            }
            Debug.Assert(_rentedBuffer.Length > 0);
            return _rentedBuffer;
        }
    }
}
