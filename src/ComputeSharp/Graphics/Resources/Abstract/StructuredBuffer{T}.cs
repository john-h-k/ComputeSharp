using ComputeSharp.Graphics.Commands;
using ComputeSharp.Graphics.Helpers;
using Microsoft.Toolkit.Diagnostics;
using System.Buffers;
using System.Runtime.CompilerServices;
using Voltium.Core;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;

namespace ComputeSharp.Resources
{
    /// <summary>
    /// A <see langword="class"/> representing a typed structured buffer stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    public abstract class StructuredBuffer<T> : Buffer<T>
        where T : unmanaged
    {
        /// <summary>
        /// Creates a new <see cref="StructuredBuffer{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="length">The number of items to store in the current buffer.</param>
        /// <param name="resourceType">The buffer type for the current buffer.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        private protected unsafe StructuredBuffer(GraphicsDevice device, int length, ResourceType resourceType, AllocationMode allocationMode)
            : base(device, length, (uint)sizeof(T), resourceType, allocationMode)
        {
        }

        /// <inheritdoc/>
        internal override unsafe void CopyTo(ref T destination, int length, int offset)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(offset, 0, Length, nameof(offset));
            Guard.IsLessThanOrEqualTo(offset + length, Length, nameof(length));

            if (GraphicsDevice.IsCacheCoherentUMA)
            {
                var pointer = this.device.Map(this.Resource);

                fixed (void* destinationPointer = &destination)
                {
                    MemoryHelper.Copy(
                        pointer,
                        (uint)offset,
                        (uint)length,
                        (uint)sizeof(T),
                        destinationPointer);
                }
            }
            else
            {
                uint
                    byteOffset = (uint)(offset * sizeof(T)),
                    byteLength = (uint)(length * sizeof(T));

                var desc = new BufferDesc { Length = (ulong)SizeInBytes, ResourceFlags = ResourceFlags.None };
                var intermediate = device.AllocateBuffer(desc, MemoryAccess.CpuReadback);

                var commandList = CommandList.Create();

                commandList.CopyBuffer(this.Resource, byteOffset, intermediate, 0, byteLength);

                this.GraphicsDevice.ExecuteCopy(commandList.Buffer).Block();

                var pointer = this.device.Map(intermediate);

                fixed (void* destinationPointer = &destination)
                {
                    MemoryHelper.Copy(
                        pointer,
                        0u,
                        (uint)length,
                        (uint)sizeof(T),
                        destinationPointer);
                }
            }
        }

        /// <summary>
        /// Reads the contents of the specified range from the current <see cref="StructuredBuffer{T}"/> instance and writes them into a target <see cref="ReadBackBuffer{T}"/> instance.
        /// </summary>
        /// <param name="destination">The target <see cref="ReadBackBuffer{T}"/> instance to write data to.</param>
        /// <param name="destinationOffset">The starting offset within <paramref name="destination"/> to write data to.</param>
        /// <param name="length">The number of items to read.</param>
        /// <param name="offset">The offset to start reading data from.</param>
        internal unsafe void CopyTo(ReadBackBuffer<T> destination, int destinationOffset, int length, int offset)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            destination.ThrowIfDeviceMismatch(GraphicsDevice);
            destination.ThrowIfDisposed();

            Guard.IsInRange(offset, 0, Length, nameof(offset));
            Guard.IsLessThanOrEqualTo(offset + length, Length, nameof(length));
            Guard.IsInRange(destinationOffset, 0, destination.Length, nameof(destinationOffset));
            Guard.IsLessThanOrEqualTo(destinationOffset + length, destination.Length, nameof(length));

            if (GraphicsDevice.IsCacheCoherentUMA)
            {
                var pointer = this.device.Map(this.Resource);

                MemoryHelper.Copy(
                    pointer,
                    (uint)offset,
                    (uint)length,
                    (uint)sizeof(T),
                    destination.MappedData + destinationOffset);
            }
            else
            {
                uint
                    byteDestinationOffset = (uint)destinationOffset * (uint)sizeof(T),
                    byteOffset = (uint)offset * (uint)sizeof(T),
                    byteLength = (uint)length * (uint)sizeof(T);

                var commandList = CommandList.Create();

                commandList.CopyBuffer(this.Resource, byteOffset, destination.Resource, byteDestinationOffset, byteLength);

                this.GraphicsDevice.ExecuteCopy(commandList.Buffer).Block();
            }
        }

        /// <inheritdoc/>
        internal override unsafe void CopyFrom(ref T source, int length, int offset)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(offset, 0, Length, nameof(offset));
            Guard.IsLessThanOrEqualTo(offset + length, Length, nameof(length));

            if (GraphicsDevice.IsCacheCoherentUMA)
            {
                var pointer = this.device.Map(this.Resource);

                fixed (void* sourcePointer = &source)
                {
                    MemoryHelper.Copy(
                        sourcePointer,
                        (uint)offset,
                        (uint)length,
                        (uint)sizeof(T),
                         pointer);
                }
            }
            else
            {
                uint
                    byteOffset = (uint)(offset * sizeof(T)),
                    byteLength = (uint)(length * sizeof(T));


                var desc = new BufferDesc { Length = (ulong)SizeInBytes, ResourceFlags = ResourceFlags.None };
                var intermediate = device.AllocateBuffer(desc, MemoryAccess.CpuReadback);

                fixed (void* sourcePointer = &source)
                {
                    var pointer = this.device.Map(intermediate);

                    MemoryHelper.Copy(
                        sourcePointer,
                        0u,
                        (uint)length,
                        (uint)sizeof(T),
                        pointer);
                }

                var commandList = CommandList.Create();

                commandList.CopyBuffer(intermediate, 0, Resource, byteOffset, byteLength);

                this.GraphicsDevice.ExecuteCopy(commandList.Buffer).Block();
            }
        }

        /// <summary>
        /// Reads the contents of the specified range from an input <see cref="ReadBackBuffer{T}"/> instance and writes them to the current the current <see cref="StructuredBuffer{T}"/> instance.
        /// </summary>
        /// <param name="source">The input <see cref="UploadBuffer{T}"/> instance to read data from.</param>
        /// <param name="sourceOffset">The starting offset within <paramref name="source"/> to read data from.</param>
        /// <param name="length">The number of items to read.</param>
        /// <param name="offset">The offset to start reading writing data to.</param>
        internal unsafe void CopyFrom(UploadBuffer<T> source, int sourceOffset, int length, int offset)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            source.ThrowIfDeviceMismatch(GraphicsDevice);
            source.ThrowIfDisposed();

            Guard.IsInRange(offset, 0, Length, nameof(offset));
            Guard.IsLessThanOrEqualTo(offset + length, Length, nameof(length));
            Guard.IsInRange(sourceOffset, 0, source.Length, nameof(sourceOffset));
            Guard.IsLessThanOrEqualTo(sourceOffset + length, source.Length, nameof(length));

            if (GraphicsDevice.IsCacheCoherentUMA)
            {
                var pointer = this.device.Map(this.Resource);

                MemoryHelper.Copy(
                    source.MappedData,
                    (uint)sourceOffset,
                    (uint)length,
                    (uint)sizeof(T),
                    (T*)pointer + offset);
            }
            else
            {
                uint
                    byteSourceOffset = (uint)sourceOffset * (uint)sizeof(T),
                    byteOffset = (uint)offset * (uint)sizeof(T),
                    byteLength = (uint)length * (uint)sizeof(T);

                var commandList = CommandList.Create();

                commandList.CopyBuffer(source.Resource, byteSourceOffset, Resource, byteOffset, byteLength);

                this.GraphicsDevice.ExecuteCopy(commandList.Buffer).Block();
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

            if (!source.IsPaddingPresent)
            {

                var commandList = CommandList.Create();

                commandList.CopyBuffer(source.Resource, Resource, (uint)SizeInBytes);

                this.GraphicsDevice.ExecuteCopy(commandList.Buffer).Block();
            }
            else CopyFromWithCpuBuffer(source);
        }
    }
}
