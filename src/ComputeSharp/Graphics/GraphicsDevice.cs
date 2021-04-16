using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ComputeSharp.Graphics.Helpers;
using ComputeSharp.Interop;
using Microsoft.Toolkit.Diagnostics;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace ComputeSharp
{
    /// <summary>
    /// A <see langword="class"/> that represents an <see cref="ID3D12Device"/> instance that can be used to run compute shaders.
    /// </summary>
    [DebuggerDisplay("{ToString(),raw}")]
    public sealed unsafe class GraphicsDevice : NativeObject
    {
        public const int Max2DTextureDimensionSize = 16384;
        public const int Max3DTextureDimensionSize = 2048;
        public const int MaxThreadGroupsPerDimension = 65535;
        public const int MaxConstantBufferElementCount = 4096;

        /// <summary>
        /// The <see cref="INativeQueue"/> instance to use for compute operations.
        /// </summary>
        private INativeQueue computeQueue;

        /// <summary>
        /// The <see cref="INativeQueue"/> instance to use for copy operations.
        /// </summary>
        private INativeQueue copyQueue;

        /// <summary>
        /// Creates a new <see cref="GraphicsDevice"/> instance for the input <see cref="INativeDevice"/>.
        /// </summary>
        /// <param name="device">The <see cref="INativeDevice"/> to use for the new <see cref="GraphicsDevice"/> instance.</param>
        internal GraphicsDevice(INativeDevice device, DXGI_ADAPTER_DESC1* desc) : base(device)
        {
            this.device = device;
            this.computeQueue = device.CreateQueue(ExecutionEngine.Compute);
            this.copyQueue = device.CreateQueue(ExecutionEngine.Copy);

            ComputeUnits = device.Info.WaveCount;
            WavefrontSize = device.Info.WaveLaneCount;

            IsCacheCoherentUMA = device.Info.IsCacheCoherentUma;

            Luid = Luid.FromLUID(desc->AdapterLuid);
            var index = new Span<char>(desc->Description, 128).IndexOf('\0');
            Name = new((char*)desc->Description, 0, index == -1 ? 128 : index);
            DedicatedMemorySize = desc->DedicatedVideoMemory;
            SharedMemorySize = desc->SharedSystemMemory;
            IsHardwareAccelerated = (desc->Flags & (uint)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0;
        }

        /// <summary>
        /// Gets the locally unique identifier for the current device.
        /// </summary>
        public Luid Luid { get; }

        /// <summary>
        /// Gets the name of the current <see cref="GraphicsDevice"/> instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the size of the dedicated memory for the current device.
        /// </summary>
        public nuint DedicatedMemorySize { get; }

        /// <summary>
        /// Gets the size of the shared system memory for the current device.
        /// </summary>
        public nuint SharedMemorySize { get; }

        /// <summary>
        /// Gets whether or not the current device is hardware accelerated.
        /// This value is <see langword="false"/> for software fallback devices.
        /// </summary>
        public bool IsHardwareAccelerated { get; }

        /// <summary>
        /// Gets the number of total lanes on the current device (eg. CUDA cores on an nVidia GPU).
        /// </summary>
        public uint ComputeUnits { get; }

        /// <summary>
        /// Gets the number of lanes in a SIMD wave on the current device (also known as "wavefront size" or "warp width").
        /// </summary>
        public uint WavefrontSize { get; }

        /// <summary>
        /// Gets whether or not the current device has a cache coherent UMA architecture.
        /// </summary>
        internal bool IsCacheCoherentUMA { get; }

        /// <summary>
        /// Checks whether the current device supports the creation of
        /// <see cref="ReadOnlyTexture2D{T}"/> resources for a specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of values to check support for.</typeparam>
        /// <returns>Whether <see cref="ReadOnlyTexture2D{T}"/> instances can be created by the current device.</returns>
        [Pure]
        public bool IsReadOnlyTexture2DSupportedForType<T>()
            where T : unmanaged
        {
            ThrowIfDisposed();

            return this.NativeDevice.SupportsFormat(DataFormatHelper.GetForType<T>(), FormatSupport.Tex2D);
        }

        /// <summary>
        /// Checks whether the current device supports the creation of
        /// <see cref="ReadWriteTexture2D{T}"/> resources for a specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of values to check support for.</typeparam>
        /// <returns>Whether <see cref="ReadWriteTexture2D{T}"/> instances can be created by the current device.</returns>
        [Pure]
        public bool IsReadWriteTexture2DSupportedForType<T>()
            where T : unmanaged
        {
            ThrowIfDisposed();

            return this.NativeDevice.SupportsFormat(
                DataFormatHelper.GetForType<T>(),
                FormatSupport.Tex2D | FormatSupport.TypedUnorderedAccess);
        }

        /// <summary>
        /// Checks whether the current device supports the creation of
        /// <see cref="ReadOnlyTexture3D{T}"/> resources for a specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of values to check support for.</typeparam>
        /// <returns>Whether <see cref="ReadOnlyTexture3D{T}"/> instances can be created by the current device.</returns>
        [Pure]
        public bool IsReadOnlyTexture3DSupportedForType<T>()
            where T : unmanaged
        {
            ThrowIfDisposed();

            return this.NativeDevice.SupportsFormat(DataFormatHelper.GetForType<T>(), FormatSupport.Tex3D);
        }

        /// <summary>
        /// Checks whether the current device supports the creation of
        /// <see cref="ReadWriteTexture3D{T}"/> resources for a specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of values to check support for.</typeparam>
        /// <returns>Whether <see cref="ReadWriteTexture3D{T}"/> instances can be created by the current device.</returns>
        [Pure]
        public bool IsReadWriteTexture3DSupportedForType<T>()
            where T : unmanaged
        {
            ThrowIfDisposed();

            return this.NativeDevice.SupportsFormat(
                DataFormatHelper.GetForType<T>(),
                FormatSupport.Tex3D | FormatSupport.TypedUnorderedAccess);
        }

        internal GpuTask ExecuteCopy(CommandBuffer buff)
        => this.copyQueue.Execute(MemoryMarshal.CreateSpan(ref buff, 1), default);

        internal GpuTask ExecuteCompute(CommandBuffer buff)
        => this.copyQueue.Execute(MemoryMarshal.CreateSpan(ref buff, 1), default);

        /// <inheritdoc/>
        protected override bool OnDispose()
        {
            if (DeviceHelper.GetDefaultDeviceLuid() == Luid)
            {
                return false;
            }

            DeviceHelper.NotifyDisposedDevice(this);

            this.copyQueue.Dispose();
            this.computeQueue.Dispose();
            this.device.Dispose();

            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Luid}] {Name}";
        }
    }
}
