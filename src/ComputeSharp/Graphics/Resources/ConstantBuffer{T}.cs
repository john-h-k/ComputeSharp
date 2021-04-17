using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using ComputeSharp.Core.Helpers;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Helpers;
using ComputeSharp.Graphics.Resources.Enums;
using ComputeSharp.Resources;
using ComputeSharp.Resources.Debug;
using Microsoft.Toolkit.Diagnostics;

namespace ComputeSharp
{
    /// <summary>
    /// A <see langword="class"/> representing a typed read write buffer stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    [DebuggerTypeProxy(typeof(BufferDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public sealed class ConstantBuffer<T> : Buffer<T>
        where T : unmanaged
    {
        /// <summary>
        /// The alignment boundary for elements in a constant buffer.
        /// </summary>
        private const int ElementAlignment = 16;

        /// <summary>
        /// Creates a new <see cref="ConstantBuffer{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="length">The number of items to store in the current buffer.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        internal ConstantBuffer(GraphicsDevice device, int length, AllocationMode allocationMode)
            : base(device, length, (uint)GetPaddedSize(), ResourceType.Constant, allocationMode)
        {
        }

        /// <summary>
        /// Gets a single <typeparamref name="T"/> value from the current constant buffer.
        /// </summary>
        /// <param name="i">The index of the value to get.</param>
        /// <remarks>This API can only be used from a compute shader, and will always throw if used anywhere else.</remarks>
        public T this[int i] => throw new InvalidExecutionContextException($"{typeof(ConstantBuffer<T>)}[{typeof(int)}]");

        /// <summary>
        /// Gets the right padded size for <typeparamref name="T"/> elements to store in the current instance.
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int GetPaddedSize()
        {
            return AlignmentHelper.Pad(sizeof(T), ElementAlignment);
        }

        /// <inheritdoc/>
        internal override unsafe void CopyTo(ref T destination, int size, int offset)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(offset, 0, Length, nameof(offset));
            Guard.IsLessThanOrEqualTo(offset + size, Length, nameof(size));

            var pointer = this.device.Map(this.Resource);

            fixed (void* destinationPointer = &destination)
            {
                if (IsPaddingPresent)
                {
                    MemoryHelper.Copy<T>(
                        pointer,
                        (uint)offset,
                        (uint)size,
                        (uint)GetPaddedSize(),
                        destinationPointer);
                }
                else
                {
                    MemoryHelper.Copy(
                        pointer,
                        (uint)offset,
                        (uint)size,
                        (uint)sizeof(T),
                        destinationPointer);
                }
            }
        }

        /// <inheritdoc/>
        internal override unsafe void CopyFrom(ref T source, int size, int offset)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(offset, 0, Length, nameof(offset));
            Guard.IsLessThanOrEqualTo(offset + size, Length, nameof(size));

            var pointer = this.device.Map(this.Resource);

            fixed (void* sourcePointer = &source)
            {
                if (IsPaddingPresent)
                {
                    MemoryHelper.Copy<T>(
                        sourcePointer,
                        pointer,
                        (uint)offset,
                        (uint)size,
                        (uint)GetPaddedSize(),
                        isPaddingInSourceToo: false);
                }
                else
                {
                    MemoryHelper.Copy(
                        sourcePointer,
                        (uint)offset,
                        (uint)size,
                        (uint)sizeof(T),
                        pointer);
                }
            }
        }

        /// <inheritdoc/>
        public override unsafe void CopyFrom(Buffer<T> source)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            source.ThrowIfDeviceMismatch(GraphicsDevice);
            source.ThrowIfDisposed();

            Guard.IsLessThanOrEqualTo(source.Length, Length, nameof(Length));

            if (source is ConstantBuffer<T> buffer)
            {

                var sourcePointer = this.device.Map(source.Resource);
                var destinationPointer = this.device.Map(this.Resource);

                if (IsPaddingPresent)
                {
                    MemoryHelper.Copy<T>(
                        sourcePointer,
                        destinationPointer,
                        0,
                        (uint)source.Length,
                        (uint)GetPaddedSize(),
                        isPaddingInSourceToo: true);
                }
                else
                {
                    MemoryHelper.Copy(
                        sourcePointer,
                        0,
                        (uint)source.Length,
                        (uint)sizeof(T),
                        destinationPointer);
                }
            }
            else CopyFromWithCpuBuffer(source);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ComputeSharp.ConstantBuffer<{typeof(T)}>[{Length}]";
        }
    }
}
