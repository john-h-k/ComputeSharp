using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Resources.Enums;
using ComputeSharp.Interop;
using Microsoft.Toolkit.Diagnostics;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;

namespace ComputeSharp.Resources
{
    internal static class INativeDeviceExtensions
    {
        public static DescriptorSetHandle CreateDescriptor(this INativeDevice device, BufferHandle buffer, ResourceType type)
        {
            var view = device.CreateViewSet(1);
            var descriptor = device.CreateDescriptorSet(type switch
            {
                ResourceType.Constant => DescriptorType.ConstantBuffer,
                ResourceType.ReadOnly => DescriptorType.StructuredBuffer,
                ResourceType.ReadWrite => DescriptorType.WritableStructuredBuffer,
                _ => 0
            }, 1);
            _ = device.CreateView(view, 0, buffer);
            device.UpdateDescriptors(view, 0, descriptor, 0, 1);
            device.DisposeViewSet(view);
            return descriptor;
        }

        public static DescriptorSetHandle CreateDescriptor(this INativeDevice device, TextureHandle texture, ResourceType type)
        {
            var view = device.CreateViewSet(1);
            var descriptor = device.CreateDescriptorSet(type switch
            {
                ResourceType.ReadOnly => DescriptorType.Texture,
                ResourceType.ReadWrite => DescriptorType.WritableTexture,
                _ => 0
            }, 1);
            _ = device.CreateView(view, 0, texture);
            device.UpdateDescriptors(view, 0, descriptor, 0, 1);
            device.DisposeViewSet(view);
            return descriptor;
        }
    }

    /// <summary>
    /// A <see langword="class"/> representing a typed buffer stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    public unsafe abstract class Buffer<T> : NativeObject
        where T : unmanaged
    {
        /// <summary>
        /// The <see cref="BufferHandle"/> instance currently mapped.
        /// </summary>
        private BufferHandle resource;

        private DescriptorSetHandle descriptor;

        /// <summary>
        /// The size in bytes of the current buffer (this value is never negative).
        /// </summary>
        protected readonly nint SizeInBytes;

        /// <summary>
        /// The <see cref="D3D12MA_Allocation"/> instance used to retrieve <see cref="d3D12Resource"/>, if any.
        /// </summary>
        /// <remarks>This will be <see langword="null"/> if the owning device has <see cref="GraphicsDevice.IsCacheCoherentUMA"/> set.</remarks>
        private UniquePtr<D3D12MA_Allocation> allocation;

        /// <summary>
        /// Creates a new <see cref="Buffer{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="length">The number of items to store in the current buffer.</param>
        /// <param name="elementSizeInBytes">The size in bytes of each buffer item (including padding, if any).</param>
        /// <param name="resourceType">The resource type for the current buffer.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        private protected Buffer(GraphicsDevice device, int length, uint elementSizeInBytes, ResourceType resourceType, AllocationMode allocationMode) : base(device.NativeDevice)
        {
            device.ThrowIfDisposed();

            if (resourceType == ResourceType.Constant)
            {
                Guard.IsBetweenOrEqualTo(length, 1, GraphicsDevice.MaxConstantBufferElementCount, nameof(length));
            }
            else
            {
                // The maximum length is set such that the aligned buffer size can't exceed uint.MaxValue
                Guard.IsBetweenOrEqualTo(length, 1, (uint.MaxValue / elementSizeInBytes) & ~255, nameof(length));
            }

            if (TypeInfo<T>.IsDoubleOrContainsDoubles &&
                device.D3D12Device->CheckFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS>(D3D12_FEATURE_D3D12_OPTIONS).DoublePrecisionFloatShaderOps == 0)
            {
                UnsupportedDoubleOperationsException.Throw<T>();
            }

            SizeInBytes = checked((nint)(length * elementSizeInBytes));
            GraphicsDevice = device;
            Length = length;

            var desc = new BufferDesc { Length = (ulong)SizeInBytes, ResourceFlags = resourceType.AsResourceFlags() };

            this.resource = device.NativeDevice.AllocateBuffer(desc, resourceType.AsMemoryAccess());
            this.descriptor = device.NativeDevice.CreateDescriptor(this.resource, resourceType);
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
        /// Gets whether or not there is some padding between elements in the current buffer.
        /// </summary>
        internal bool IsPaddingPresent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SizeInBytes > (nint)Length * sizeof(T);
        }

        /// <summary>
        /// Gets the <see cref="BufferHandle"/> instance currently mapped.
        /// </summary>
        internal BufferHandle Resource => this.resource;

        /// <summary>
        /// Gets the <see cref="DescriptorSetHandle"/> instance currently mapped.
        /// </summary>
        internal DescriptorSetHandle Descriptor => this.descriptor;

        /// <summary>
        /// Reads the contents of the specified range from the current <see cref="Buffer{T}"/> instance and writes them into a target memory area.
        /// </summary>
        /// <param name="destination">The input memory area to write data to.</param>
        /// <param name="length">The length of the memory area to write data to.</param>
        /// <param name="offset">The offset to start reading data from.</param>
        internal abstract void CopyTo(ref T destination, int length, int offset);

        /// <summary>
        /// Writes the contents of a given memory area to a specified area of the current <see cref="Buffer{T}"/> instance.
        /// </summary>
        /// <param name="source">The input memory area to read data from.</param>
        /// <param name="length">The length of the input memory area to read data from.</param>
        /// <param name="offset">The offset to start writing data to.</param>
        internal abstract void CopyFrom(ref T source, int length, int offset);

        /// <summary>
        /// Writes the contents of a given <see cref="Buffer{T}"/> to the current <see cref="Buffer{T}"/> instance.
        /// </summary>
        /// <param name="source">The input <see cref="Buffer{T}"/> to read data from.</param>
        public abstract void CopyFrom(Buffer<T> source);

        /// <summary>
        /// Writes the contents of a given <see cref="Buffer{T}"/> to the current <see cref="Buffer{T}"/> instance, using a temporary CPU buffer.
        /// </summary>
        /// <param name="source">The input <see cref="Buffer{T}"/> to read data from.</param>
        protected void CopyFromWithCpuBuffer(Buffer<T> source)
        {
            T[] array = ArrayPool<T>.Shared.Rent(source.Length);

            try
            {
                ref T r0 = ref MemoryMarshal.GetArrayDataReference(array);

                source.CopyTo(ref r0, source.Length, 0);

                CopyFrom(ref r0, source.Length, 0);
            }
            finally
            {
                ArrayPool<T>.Shared.Return(array);
            }
        }

        /// <inheritdoc/>
        protected override bool OnDispose()
        {
            this?.NativeDevice.DisposeBuffer(this.resource);

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
    }
}
