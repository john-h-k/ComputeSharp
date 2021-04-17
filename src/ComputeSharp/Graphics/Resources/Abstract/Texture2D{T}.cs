using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using ComputeSharp.__Internals;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Commands;
using ComputeSharp.Graphics.Helpers;
using ComputeSharp.Graphics.Resources.Enums;
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
    public struct TextureFootprint
    {
        public DataFormat Format;
        public uint Width, Height, Depth;
        public uint RowSize, RowPitch;
    }

    /// <summary>
    /// A <see langword="class"/> representing a typed 2D texture stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the texture.</typeparam>
    public unsafe abstract class Texture2D<T> : NativeObject, GraphicsResourceHelper.IGraphicsResource
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
        /// Creates a new <see cref="Texture2D{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="ComputeSharp.GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="resourceType">The resource type for the current texture.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        /// <param name="formatSupport">The format support for the current texture type.</param>
        private protected Texture2D(GraphicsDevice device, int width, int height, ResourceType resourceType, AllocationMode allocationMode, FormatSupport formatSupport) : base(device.NativeDevice)
        {
            device.ThrowIfDisposed();

            Guard.IsBetweenOrEqualTo(width, 1, GraphicsDevice.Max2DTextureDimensionSize, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, GraphicsDevice.Max2DTextureDimensionSize, nameof(height));

            if (!device.NativeDevice.SupportsFormat(DataFormatHelper.GetForType<T>(), formatSupport))
            {
                UnsupportedTextureTypeException.ThrowForTexture2D<T>();
            }

            GraphicsDevice = device;

            var desc = new TextureDesc
            {
                Dimension = TextureDimension.Tex1D,
                Width = (ulong)width,
                Height = (uint)height,
                DepthOrArraySize = 1,
                Format = DataFormatHelper.GetForType<T>(),
                Layout = TextureLayout.Optimal,
                MipCount = 1,
                ResourceFlags = resourceType.AsResourceFlags()
            };

            this.resource = this.device.AllocateTexture(
                desc,
                 resourceType.InitialResourceState());

            this.resourceState = resourceType.InitialResourceState();

            this.descriptor = device.NativeDevice.CreateDescriptor(this.resource, resourceType);
            this.useCopy = resourceType != ResourceType.ReadWrite;

            this.footprint = new TextureFootprint
            {
                Format = DataFormatHelper.GetForType<T>(),
                Width = (uint)width,
                Height = (uint)height,
                Depth = 1,
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
        /// Gets the <see cref="TextureHandle"/> instance currently mapped.
        /// </summary>
        internal TextureHandle Resource => this.resource;

        /// <summary>
        /// Gets the <see cref="DescriptorSetHandle"/> instance currently mapped.
        /// </summary>
        internal DescriptorSetHandle Descriptor => this.descriptor;

        /// <summary>
        /// Reads the contents of the specified range from the current <see cref="Texture2D{T}"/> instance and writes them into a target memory area.
        /// </summary>
        /// <param name="destination">The target memory area to write data to.</param>
        /// <param name="size">The size of the target memory area to write data to.</param>
        /// <param name="x">The horizontal offset in the source texture.</param>
        /// <param name="y">The vertical offset in the source texture.</param>
        /// <param name="width">The width of the memory area to copy.</param>
        /// <param name="height">The height of the memory area to copy.</param>
        internal void CopyTo(ref T destination, int size, int x, int y, int width, int height)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(x, 0, Width, nameof(x));
            Guard.IsInRange(y, 0, Height, nameof(y));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsLessThanOrEqualTo(x + width, Width, nameof(x));
            Guard.IsLessThanOrEqualTo(y + height, Height, nameof(y));
            Guard.IsGreaterThanOrEqualTo(size, (nint)width * height, nameof(size));

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
                    sourceZ: 0);


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
                    this.footprint.RowSize,
                    this.footprint.RowPitch,
                    destinationPointer);
            }

            this.device.DisposeBuffer(intermediate);
        }

        /// <summary>
        /// Reads the contents of the specified range from the current <see cref="Texture2D{T}"/> instance and writes them into a target <see cref="ReadBackTexture2D{T}"/> instance.
        /// </summary>
        /// <param name="destination">The target <see cref="ReadBackTexture2D{T}"/> instance to write data to.</param>
        /// <param name="destinationX">The horizontal offset within <paramref name="destination"/>.</param>
        /// <param name="destinationY">The vertical offset within <paramref name="destination"/>.</param>
        /// <param name="sourceX">The horizontal offset in the source texture.</param>
        /// <param name="sourceY">The vertical offset in the source texture.</param>
        /// <param name="width">The width of the memory area to copy.</param>
        /// <param name="height">The height of the memory area to copy.</param>
        internal void CopyTo(ReadBackTexture2D<T> destination, int destinationX, int destinationY, int sourceX, int sourceY, int width, int height)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            destination.ThrowIfDeviceMismatch(GraphicsDevice);
            destination.ThrowIfDisposed();

            Guard.IsInRange(destinationX, 0, destination.Width, nameof(destinationX));
            Guard.IsInRange(destinationY, 0, destination.Height, nameof(destinationY));
            Guard.IsInRange(sourceX, 0, Width, nameof(sourceX));
            Guard.IsInRange(sourceY, 0, Height, nameof(sourceY));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsBetweenOrEqualTo(width, 1, destination.Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, destination.Height, nameof(height));
            Guard.IsBetweenOrEqualTo(destinationX + width, 1, destination.Width, nameof(destinationX));
            Guard.IsBetweenOrEqualTo(destinationY + height, 1, destination.Height, nameof(destinationY));
            Guard.IsLessThanOrEqualTo(sourceX + width, Width, nameof(sourceX));
            Guard.IsLessThanOrEqualTo(sourceY + height, Height, nameof(sourceY));

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
                destZ: 0,
                (uint)sourceX,
                (uint)sourceY,
                sourceZ: 0);

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, ResourceState.CopySource, this.resourceState);
            }

            var task = this.useCopy ? this.GraphicsDevice.ExecuteCopy(copyCommandList.Buffer) : this.GraphicsDevice.ExecuteCompute(copyCommandList.Buffer);
            task.Block();
        }

        /// <summary>
        /// Writes the contents of a given memory area to a specified area of the current <see cref="Texture2D{T}"/> instance.
        /// </summary>
        /// <param name="source">The input memory area to read data from.</param>
        /// <param name="size">The size of the memory area to read data from.</param>
        /// <param name="x">The horizontal offset in the destination texture.</param>
        /// <param name="y">The vertical offset in the destination texture.</param>
        /// <param name="width">The width of the memory area to write to.</param>
        /// <param name="height">The height of the memory area to write to.</param>
        internal void CopyFrom(ref T source, int size, int x, int y, int width, int height)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            Guard.IsInRange(x, 0, Width, nameof(x));
            Guard.IsInRange(y, 0, Height, nameof(y));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsLessThanOrEqualTo(x + width, Width, nameof(x));
            Guard.IsLessThanOrEqualTo(y + height, Height, nameof(y));
            Guard.IsGreaterThanOrEqualTo(size, (nint)width * height, nameof(size));

            var desc = new BufferDesc { Length = this.bufferSize, ResourceFlags = ResourceFlags.None };
            var intermediate = this.device.AllocateBuffer(desc, MemoryAccess.CpuUpload);

            var pointer = this.device.Map(intermediate);

            fixed (void* sourcePointer = &source)
            {
                MemoryHelper.Copy(
                    sourcePointer,
                    pointer,
                    (uint)height,
                    this.footprint.RowSize,
                    this.footprint.RowPitch);
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
                destZ: 0,
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
        /// Writes the contents of a given <see cref="UploadTexture2D{T}"/> instance to a specified area of the current <see cref="Texture2D{T}"/> instance.
        /// </summary>
        /// <param name="source">The input <see cref="UploadTexture2D{T}"/> instance to read data from.</param>
        /// <param name="sourceX">The horizontal offset within <paramref name="source"/>.</param>
        /// <param name="sourceY">The vertical offset within <paramref name="source"/>.</param>
        /// <param name="destinationX">The horizontal offset in the destination texture.</param>
        /// <param name="destinationY">The vertical offset in the destination texture.</param>
        /// <param name="width">The width of the memory area to write to.</param>
        /// <param name="height">The height of the memory area to write to.</param>
        internal void CopyFrom(UploadTexture2D<T> source, int sourceX, int sourceY, int destinationX, int destinationY, int width, int height)
        {
            GraphicsDevice.ThrowIfDisposed();

            ThrowIfDisposed();

            source.ThrowIfDeviceMismatch(GraphicsDevice);
            source.ThrowIfDisposed();

            Guard.IsInRange(sourceX, 0, source.Width, nameof(sourceX));
            Guard.IsInRange(sourceY, 0, source.Height, nameof(sourceY));
            Guard.IsInRange(destinationX, 0, Width, nameof(destinationX));
            Guard.IsInRange(destinationY, 0, Height, nameof(destinationY));
            Guard.IsBetweenOrEqualTo(width, 1, Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, Height, nameof(height));
            Guard.IsBetweenOrEqualTo(width, 1, source.Width, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, source.Height, nameof(height));
            Guard.IsLessThanOrEqualTo(sourceX + width, source.Width, nameof(sourceX));
            Guard.IsLessThanOrEqualTo(sourceY + height, source.Height, nameof(sourceY));
            Guard.IsLessThanOrEqualTo(destinationX + width, Width, nameof(destinationX));
            Guard.IsLessThanOrEqualTo(destinationY + height, Height, nameof(destinationY));

            var copyCommandList = CommandList.Create();

            if (!this.useCopy)
            {
                copyCommandList.ResourceTransition(this.resource, this.resourceState, ResourceState.CopyDestination);
            }

            copyCommandList.CopyBufferToTexture(
                this.footprint.Format,
                source.Resource,
                offset: 0,
                Resource,
                this.footprint,
                subresource: 0,
                (uint)destinationX,
                (uint)destinationY,
                destZ: 0,
                (uint)sourceX,
                (uint)sourceY,
                sourceZ: 0);

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
            this.device.DisposeTexture(this.resource);

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
