using System.Runtime.CompilerServices;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Helpers;
using ComputeSharp.Graphics.Resources.Enums;
using ComputeSharp.Interop;
using Microsoft.Toolkit.Diagnostics;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;

namespace ComputeSharp.Resources
{
    /// <summary>
    /// A <see langword="class"/> representing a typed 2D texture stored on on CPU memory, that can be used to transfer data to/from the GPU.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the texture.</typeparam>
    public unsafe abstract class TransferTexture2D<T> : NativeObject
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
        /// Creates a new <see cref="TransferTexture2D{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="ComputeSharp.GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="resourceType">The resource type for the current texture.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        private protected TransferTexture2D(GraphicsDevice device, int width, int height, ResourceType resourceType, AllocationMode allocationMode) : base(device.NativeDevice)
        {
            device.ThrowIfDisposed();

            Guard.IsBetweenOrEqualTo(width, 1, GraphicsDevice.Max2DTextureDimensionSize, nameof(width));
            Guard.IsBetweenOrEqualTo(height, 1, GraphicsDevice.Max2DTextureDimensionSize, nameof(height));

            if (!device.NativeDevice.SupportsFormat(DataFormatHelper.GetForType<T>(), FormatSupport.Tex2D))
            {
                UnsupportedTextureTypeException.ThrowForTexture2D<T>();
            }

            GraphicsDevice = device;


            this.Width = width;
            this.Height = height;

            var desc = new BufferDesc { Length = DataFormatHelper.AlignedRowPitch<T>((uint)width) * (uint)height, ResourceFlags = ResourceFlags.None };
            this.resource = this.device.AllocateBuffer(desc, resourceType.AsMemoryAccess());
            this.mappedData = (T*)this.device.Map(this.resource);
        }

        /// <summary>
        /// Gets the <see cref="ComputeSharp.GraphicsDevice"/> associated with the current instance.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the width of the current texture.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets the height of the current texture.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets the <see cref="BufferHandle"/> instance currently mapped.
        /// </summary>
        internal BufferHandle Resource => this.resource;

        /// <inheritdoc/>
        public TextureView2D<T> View
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();

                return new(this.mappedData, Width, Height, (int)DataFormatHelper.AlignedRowPitch<T>((uint)this.Width));
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
    }
}
