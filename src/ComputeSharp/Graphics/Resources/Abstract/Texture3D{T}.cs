using System.Runtime.CompilerServices;
using ComputeSharp.__Internals;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Commands;
using ComputeSharp.Graphics.Helpers;
using ComputeSharp.Interop;
using Microsoft.Toolkit.Diagnostics;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;

#pragma warning disable CS0618

namespace ComputeSharp.Resources
{
    /// <summary>
    /// A <see langword="class"/> representing a typed 3D texture stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the texture.</typeparam>
    public unsafe abstract class Texture3D<T> : NativeObject, GraphicsResourceHelper.IGraphicsResource
        where T : unmanaged
    {
        /// <summary>
        /// The <see cref="TextureHandle"/> instance currently mapped.
        /// </summary>
        private TextureHandle resource;
        /// <summary>
        /// The <see cref="DescriptorSetHandle"/> instance currently mapped.
        /// </summary>
        private DescriptorSetHandle descriptor;

        /// <summary>
        /// The default <see cref="ResourceState"/> value for the current resource.
        /// </summary>
        private readonly ResourceState resourceState;

        /// <summary>
        /// Whether to use compute for copy operations.
        /// </summary>
        private readonly bool useCopy;


        private readonly TextureFootprint footprint;
        private readonly uint bufferSize;

        /// <summary>
        /// The <see cref="D3D12MA_Allocation"/> instance used to retrieve <see cref="d3D12Resource"/>, if any.
        /// </summary>
        /// <remarks>This will be <see langword="null"/> if the owning device has <see cref="GraphicsDevice.IsCacheCoherentUMA"/> set.</remarks>
        private UniquePtr<D3D12MA_Allocation> allocation;

        /// <summary>
        /// Creates a new <see cref="Texture3D{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="ComputeSharp.GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="depth">The depth of the texture.</param>
        /// <param name="resourceType">The resource type for the current texture.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        /// <param name="formatSupport">The format support for the current texture type.</param>
        private protected Texture3D(GraphicsDevice device, int width, int height, int depth, ResourceType resourceType, AllocationMode allocationMode, FormatSupport formatSupport) : base(device.NativeDevice)
        {
            device.ThrowIfDisposed();

            Guard.IsBetweenOrEqualTo(width, 1, GraphicsDevice.Max3DTextureDimensionSize, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, GraphicsDevice.Max3DTextureDimensionSize, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, GraphicsDevice.Max3DTextureDimensionSize, nameof(depth));

            if (!device.NativeDevice.SupportsFormat(DataFormatHelper.GetForType<T>(), formatSupport))
            {
                UnsupportedTextureTypeException.ThrowForTexture2D<T>();
            }

            GraphicsDevice = device;

            var isWrite = resourceType == ResourceType.ReadWrite;

            var desc = new TextureDesc
            {
                Dimension = TextureDimension.Tex3D,
                Width = (ulong)width,
                Height = (uint)height,
                DepthOrArraySize = (ushort)depth,
                ResourceFlags = isWrite ? ResourceFlags.AllowUnorderedAccess : ResourceFlags.None,
                Format = DataFormatHelper.GetForType<T>(),
                Layout = TextureLayout.Optimal,
                MipCount = 1
            };

            this.resource = device.NativeDevice.AllocateTexture(
                desc,
                ResourceState.Common);

            this.descriptor = device.NativeDevice.CreateDescriptor(this.resource, resourceType);
            this.useCopy = resourceType != ResourceType.ReadWrite;

            this.footprint = new TextureFootprint
            {
                Format = DataFormatHelper.GetForType<T>(),
                Width = (uint)width,
                Height = (uint)height,
                Depth = (uint)depth,
                RowSize = DataFormatHelper.RowSize<T>((uint)width),
                RowPitch = DataFormatHelper.AlignedRowPitch<T>((uint)width),
            };

            this.bufferSize = this.footprint.RowPitch * this.footprint.Height * this.footprint.Depth;
        }

        /// <summary>
        /// Gets the <see cref="ComputeSharp.GraphicsDevice"/> associated with the current instance.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the width of the current texture.
        /// </summary>
        public int Width => (int)this.footprint.Width;

        /// <summary>
        /// Gets the height of the current texture.
        /// </summary>
        public int Height => (int)this.footprint.Height;

        /// <summary>
        /// Gets the depth of the current texture.
        /// </summary>
        public int Depth => (int)this.footprint.Depth;

        /// <summary>
        /// Gets the <see cref="TextureHandle"/> instance currently mapped.
        /// </summary>
        internal TextureHandle Resource => this.resource;

        /// <summary>
        /// Gets the <see cref="DescriptorSetHandle"/> instance currently mapped.
        /// </summary>
        internal DescriptorSetHandle Descriptor => this.descriptor;

        /// <summary>
        /// Reads the contents of the specified range from the current <see cref="Texture3D{T}"/> instance and writes them into a target memory area.
        /// </summary>
        /// <param name="destination">The target memory area to write data to.</param>
        /// <param name="size">The size of the memory area to write data to.</param>
        /// <param name="x">The horizontal offset in the source texture.</param>
        /// <param name="y">The vertical offset in the source texture.</param>
        /// <param name="z">The depthwise offset in the source texture.</param>
        /// <param name="width">The width of the memory area to copy.</param>
        /// <param name="height">The height of the memory area to copy.</param>
        /// <param name="depth">The depth of the memory area to copy.</param>
        internal void CopyTo(ref T destination, int size, int x, int y, int z, int width, int height, int depth)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(x, 0, Width, nameof(x));
            Guard.IsInRange(y, 0, Height, nameof(y));
            Guard.IsInRange(z, 0, Depth, nameof(z));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, Depth, nameof(depth));
            Guard.IsLessThanOrEqualTo(x + width, Width, nameof(x));
            Guard.IsLessThanOrEqualTo(y + height, Height, nameof(y));
            Guard.IsLessThanOrEqualTo(z + depth, Depth, nameof(z));
            Guard.IsGreaterThanOrEqualTo(size, (nint)width * height * depth, nameof(size));



            var desc = new BufferDesc { Length = this.bufferSize, ResourceFlags = ResourceFlags.None };
            var intermediate = this.device.AllocateBuffer(desc, MemoryAccess.CpuReadback);

            var copyCommandList = CommandList.Create();

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, this.resourceState, ResourceState.CopySource);
            }

            copyCommandList.CopyTextureToBuffer(
                this.footprint.Format,
                this.resource,
                0,
                intermediate,
                0,
                this.footprint,
                destX: 0,
                destY: 0,
                destZ: 0,
                sourceX: (uint)x,
                sourceY: (uint)y,
                sourceZ: (ushort)z);

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, ResourceState.CopySource, this.resourceState);
            }

            var task = this.useCopy ? this.GraphicsDevice.ExecuteCopy(copyCommandList.Buffer) : this.GraphicsDevice.ExecuteCompute(copyCommandList.Buffer);
            task.Block();

            var pointer = this.device.Map(intermediate);

            fixed (void* destinationPointer = &destination)
            {
                MemoryHelper.Copy(
                    pointer,
                    (uint)height,
                    (uint)depth,
                    this.footprint.RowSize,
                    this.footprint.RowPitch,
                    this.footprint.RowPitch * (uint)height,
                    destinationPointer);
            }

            this.device.DisposeBuffer(intermediate);
        }

        /// <summary>
        /// Reads the contents of the specified range from the current <see cref="Texture3D{T}"/> instance and writes them into a <see cref="ReadBackTexture3D{T}"/> instance.
        /// </summary>
        /// <param name="destination">The target <see cref="ReadBackTexture3D{T}"/> instance to write data to.</param>
        /// <param name="destinationX">The horizontal offset within <paramref name="destination"/>.</param>
        /// <param name="destinationY">The vertical offset within <paramref name="destination"/>.</param>
        /// <param name="destinationZ">The depthwise offset within <paramref name="destination"/>.</param>
        /// <param name="sourceX">The horizontal offset in the source texture.</param>
        /// <param name="sourceY">The vertical offset in the source texture.</param>
        /// <param name="sourceZ">The depthwise offset in the source texture.</param>
        /// <param name="width">The width of the memory area to copy.</param>
        /// <param name="height">The height of the memory area to copy.</param>
        /// <param name="depth">The depth of the memory area to copy.</param>
        internal void CopyTo(ReadBackTexture3D<T> destination, int destinationX, int destinationY, int destinationZ, int sourceX, int sourceY, int sourceZ, int width, int height, int depth)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            destination.ThrowIfDeviceMismatch(GraphicsDevice);
            destination.ThrowIfDisposed();

            Guard.IsInRange(destinationX, 0, destination.Width, nameof(destinationX));
            Guard.IsInRange(destinationY, 0, destination.Height, nameof(destinationY));
            Guard.IsInRange(destinationZ, 0, destination.Depth, nameof(destinationZ));
            Guard.IsInRange(sourceX, 0, Width, nameof(sourceX));
            Guard.IsInRange(sourceY, 0, Height, nameof(sourceY));
            Guard.IsInRange(sourceZ, 0, Depth, nameof(sourceZ));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, Depth, nameof(depth));
            Guard.IsBetweenOrEqualTo(width, 1, destination.Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, destination.Height, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, destination.Depth, nameof(depth));
            Guard.IsBetweenOrEqualTo(destinationX + width, 1, destination.Width, nameof(destinationX));
            Guard.IsBetweenOrEqualTo(destinationY + height, 1, destination.Height, nameof(destinationY));
            Guard.IsBetweenOrEqualTo(destinationZ + depth, 1, destination.Depth, nameof(destinationZ));
            Guard.IsLessThanOrEqualTo(sourceX + width, Width, nameof(sourceX));
            Guard.IsLessThanOrEqualTo(sourceY + height, Height, nameof(sourceY));
            Guard.IsLessThanOrEqualTo(sourceZ + depth, Depth, nameof(sourceZ));

            var copyCommandList = CommandList.Create();

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, this.resourceState, ResourceState.CopySource);
            }

            copyCommandList.CopyTextureToBuffer(
                this.footprint.Format,
                this.resource,
                0,
                destination.Resource,
                0,
                this.footprint,
                (uint)destinationX,
                (uint)destinationY,
                (ushort)destinationZ,
                (uint)sourceX,
                (uint)sourceY,
                (ushort)sourceZ);

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, ResourceState.CopySource, this.resourceState);
            }

            var task = this.useCopy ? this.GraphicsDevice.ExecuteCopy(copyCommandList.Buffer) : this.GraphicsDevice.ExecuteCompute(copyCommandList.Buffer);
            task.Block();
        }

        /// <summary>
        /// Writes the contents of a given memory area to a specified area of the current <see cref="Texture3D{T}"/> instance.
        /// </summary>
        /// <param name="source">The input memory area to read data from.</param>
        /// <param name="size">The size of the memory area to read data from.</param>
        /// <param name="x">The horizontal offset in the destination texture.</param>
        /// <param name="y">The vertical offset in the destination texture.</param>
        /// <param name="z">The depthwise offset in the destination texture.</param>
        /// <param name="width">The width of the memory area to write to.</param>
        /// <param name="height">The height of the memory area to write to.</param>
        /// <param name="depth">The depth of the memory area to write to.</param>
        internal void CopyFrom(ref T source, int size, int x, int y, int z, int width, int height, int depth)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(x, 0, Width, nameof(x));
            Guard.IsInRange(y, 0, Height, nameof(y));
            Guard.IsInRange(z, 0, Depth, nameof(z));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, Depth, nameof(depth));
            Guard.IsLessThanOrEqualTo(x + width, Width, nameof(x));
            Guard.IsLessThanOrEqualTo(y + height, Height, nameof(y));
            Guard.IsLessThanOrEqualTo(z + depth, Depth, nameof(z));
            Guard.IsGreaterThanOrEqualTo(size, (nint)width * height * depth, nameof(size));

            var desc = new BufferDesc { Length = this.bufferSize, ResourceFlags = ResourceFlags.None };
            var intermediate = this.device.AllocateBuffer(desc, MemoryAccess.CpuUpload);

            var pointer = this.device.Map(intermediate);

            fixed (void* sourcePointer = &source)
            {
                MemoryHelper.Copy(
                    sourcePointer,
                    pointer,
                    (uint)height,
                    (uint)depth,
                    this.footprint.RowSize,
                    this.footprint.RowPitch,
                    this.footprint.RowPitch * (uint)height);
            }

            var copyCommandList = CommandList.Create();

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, this.resourceState, ResourceState.CopyDestination);
            }

            copyCommandList.CopyBufferToTexture(
                this.footprint.Format,
                intermediate,
                0,
                this.resource,
                this.footprint,
                0,
                destX: (uint)x,
                destY: (uint)y,
                destZ: (ushort)z,
                sourceX: 0,
                sourceY: 0,
                sourceZ: 0);


            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, ResourceState.CopyDestination, this.resourceState);
            }

            var task = this.useCopy ? this.GraphicsDevice.ExecuteCopy(copyCommandList.Buffer) : this.GraphicsDevice.ExecuteCompute(copyCommandList.Buffer);
            task.Block();

            this.device.DisposeBuffer(intermediate);
        }

        /// <summary>
        /// Writes the contents of a given <see cref="UploadTexture3D{T}"/> instance to a specified area of the current <see cref="Texture3D{T}"/> instance.
        /// </summary>
        /// <param name="source">The input <see cref="UploadTexture3D{T}"/> instance to read data from.</param>
        /// <param name="sourceX">The horizontal offset within <paramref name="source"/>.</param>
        /// <param name="sourceY">The vertical offset within <paramref name="source"/>.</param>
        /// <param name="sourceZ">The depthwise offset within <paramref name="source"/>.</param>
        /// <param name="destinationX">The horizontal offset in the destination texture.</param>
        /// <param name="destinationY">The vertical offset in the destination texture.</param>
        /// <param name="destinationZ">The depthwise offset in the destination texture.</param>
        /// <param name="width">The width of the memory area to write to.</param>
        /// <param name="height">The height of the memory area to write to.</param>
        /// <param name="depth">The depth of the memory area to write to.</param>
        internal void CopyFrom(UploadTexture3D<T> source, int sourceX, int sourceY, int sourceZ, int destinationX, int destinationY, int destinationZ, int width, int height, int depth)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            source.ThrowIfDeviceMismatch(GraphicsDevice);
            source.ThrowIfDisposed();

            Guard.IsInRange(sourceX, 0, source.Width, nameof(sourceX));
            Guard.IsInRange(sourceY, 0, source.Height, nameof(sourceY));
            Guard.IsInRange(sourceZ, 0, source.Depth, nameof(sourceZ));
            Guard.IsInRange(destinationX, 0, Width, nameof(destinationX));
            Guard.IsInRange(destinationY, 0, Height, nameof(destinationY));
            Guard.IsInRange(destinationZ, 0, Depth, nameof(destinationZ));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, Depth, nameof(depth));
            Guard.IsBetweenOrEqualTo(width, 1, source.Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, source.Height, nameof(height));
            Guard.IsBetweenOrEqualTo(depth, 1, source.Depth, nameof(depth));
            Guard.IsLessThanOrEqualTo(sourceX + width, source.Width, nameof(sourceX));
            Guard.IsLessThanOrEqualTo(sourceY + height, source.Height, nameof(sourceY));
            Guard.IsLessThanOrEqualTo(sourceZ + depth, source.Depth, nameof(sourceZ));
            Guard.IsLessThanOrEqualTo(destinationX + width, Width, nameof(destinationX));
            Guard.IsLessThanOrEqualTo(destinationY + height, Height, nameof(destinationY));
            Guard.IsLessThanOrEqualTo(destinationZ + depth, Depth, nameof(destinationZ));

            var copyCommandList = CommandList.Create();


            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, this.resourceState, ResourceState.CopyDestination);
            }

            copyCommandList.CopyBufferToTexture(
                this.footprint.Format,
                source.Resource,
                0,
                this.resource,
                this.footprint,
                0,
                (uint)destinationX,
                (uint)destinationY,
                (ushort)destinationZ,
                (uint)sourceX,
                (uint)sourceY,
                (ushort)sourceZ);

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, ResourceState.CopyDestination, this.resourceState);
            }

            var task = this.useCopy ? this.GraphicsDevice.ExecuteCopy(copyCommandList.Buffer) : this.GraphicsDevice.ExecuteCompute(copyCommandList.Buffer);
            task.Block();
        }

        /// <inheritdoc/>
        protected override bool OnDispose()
        {
            this?.device.DisposeTexture(this.resource);

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

        /// <inheritdoc/>
        DescriptorSetHandle GraphicsResourceHelper.IGraphicsResource.ValidateAndGetDescriptorSetHandle(GraphicsDevice device)
        {
            ThrowIfDisposed();
            ThrowIfDeviceMismatch(device);

            return this.descriptor;
        }
    }
}
