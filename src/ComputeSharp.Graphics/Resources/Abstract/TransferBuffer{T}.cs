using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using ComputeSharp.Exceptions;
using ComputeSharp.Interop;
using Microsoft.Toolkit.Diagnostics;
using Voltium.Core;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;

namespace ComputeSharp.Resources
{
    /// <summary>
    /// A <see langword="class"/> representing a typed buffer stored on CPU memory, that can be used to transfer data to/from the GPU.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    public abstract unsafe class TransferBuffer<T> : NativeObject, IMemoryOwner<T>
        where T : unmanaged
    {
        /// <summary>
        /// The <see cref="BufferHandle"/> instance currently mapped.
        /// </summary>
        private BufferHandle resource;

        /// <summary>
        /// The pointer to the start of the mapped buffer data.
        /// </summary>
        private readonly T* mappedData;

        /// <summary>
        /// Creates a new <see cref="TransferBuffer{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="length">The number of items to store in the current buffer.</param>
        /// <param name="resourceType">The resource type for the current buffer.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        private protected TransferBuffer(GraphicsDevice device, int length, ResourceType resourceType, AllocationMode allocationMode) : base(device.NativeDevice)
        {
            device.ThrowIfDisposed();

            // The maximum length is set such that the aligned buffer size can't exceed uint.MaxValue
            Guard.IsBetweenOrEqualTo(length, 1, (uint.MaxValue / (uint)sizeof(T)) & ~255, nameof(length));

            GraphicsDevice = device;
            Length = length;

            ulong sizeInBytes = (uint)length * (uint)sizeof(T);

            var desc = new BufferDesc { Length = sizeInBytes, ResourceFlags = resourceType == ResourceType.ReadWrite ? ResourceFlags.AllowUnorderedAccess : ResourceFlags.None };

            this.resource = device.NativeDevice.AllocateBuffer(desc, MemoryAccess.CpuUpload);
            this.mappedData = (T*)device.NativeDevice.Map(this.resource);
        }

        /// <summary>
        /// Gets the <see cref="ComputeSharp.GraphicsDevice"/> associated with the current instance.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the length of the current buffer.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the <see cref="BufferHandle"/> instance currently mapped.
        /// </summary>
        internal BufferHandle Resource => this.resource;

        /// <summary>
        /// Gets the pointer to the start of the mapped buffer data.
        /// </summary>
        internal T* MappedData => this.mappedData;

        /// <inheritdoc/>
        public Memory<T> Memory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();

                return new MemoryManager(this).Memory;
            }
        }

        /// <inheritdoc/>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();

                return new(this.mappedData, Length);
            }
        }

        /// <inheritdoc/>
        protected override bool OnDispose()
        {
            this.device.DisposeBuffer(this.resource);

            return true;
        }

        /// <summary>
        /// Throws a <see cref="GraphicsDeviceMismatchException"/> if the target device doesn't match the current one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfDeviceMismatch(GraphicsDevice device)
        {
            if (GraphicsDevice != device)
            {
                GraphicsDeviceMismatchException.Throw(this, device);
            }
        }

        /// <summary>
        /// A <see cref="MemoryManager{T}"/> implementation wrapping a <see cref="TransferBuffer{T}"/> instance.
        /// </summary>
        private sealed class MemoryManager : MemoryManager<T>
        {
            /// <summary>
            /// The <see cref="TransferBuffer{T}"/> in use.
            /// </summary>
            private readonly TransferBuffer<T> buffer;

            /// <summary>
            /// Creates a new <see cref="MemoryManager"/> instance for a given buffer.
            /// </summary>
            /// <param name="buffer">The <see cref="TransferBuffer{T}"/> in use.</param>
            public MemoryManager(TransferBuffer<T> buffer)
            {
                this.buffer = buffer;
            }

            /// <inheritdoc/>
            public override Memory<T> Memory
            {
                get => CreateMemory(this.buffer.Length);
            }

            /// <inheritdoc/>
            public override Span<T> GetSpan()
            {
                return this.buffer.Span;
            }

            /// <inheritdoc/>
            public override MemoryHandle Pin(int elementIndex = 0)
            {
                Guard.IsEqualTo(elementIndex, 0, nameof(elementIndex));

                this.buffer.ThrowIfDisposed();

                return new(this.buffer.mappedData);
            }

            /// <inheritdoc/>
            public override void Unpin()
            {
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}
