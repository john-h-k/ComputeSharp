using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Voltium.Core.Devices;
using Voltium.Core.NativeApi;

namespace ComputeSharp.Shaders.Translation.Models
{
    /// <summary>
    /// A <see langword="struct"/> that contains all the captured data to dispatch a shader.
    /// </summary>
    internal readonly ref struct DispatchData
    {
        private readonly INativeDevice device;
        private readonly DescriptorSetHandle[] resources;

        /// <summary>
        /// The number of resources values in <see cref="resources"/>.
        /// </summary>
        private readonly int resourcesCount;

        /// <summary>
        /// The <see cref="byte"/> array with all the captured variables, with proper padding.
        /// </summary>
        private readonly byte[] variablesArray;

        /// <summary>
        /// The actual size in bytes to use from <see cref="variablesArray"/>.
        /// </summary>
        private readonly int variablesByteSize;

        /// <summary>
        /// Creates a new <see cref="DispatchData"/> instance with the specified parameters.
        /// </summary>
        /// <param name="resourcesArray">The <see cref="ulong"/> array with the captured buffers.</param>
        /// <param name="resourcesCount">The number of <see cref="ShaderResourceBinding"/> instances in <see cref="resourcesArray"/>.</param>
        /// <param name="variablesArray">The <see cref="byte"/> array with all the captured variables, with proper padding.</param>
        /// <param name="variablesByteSize">The actual size in bytes to use from <see cref="variablesArray"/>.</param>
        public DispatchData(INativeDevice device, DescriptorSetHandle[] resources, int resourcesCount, byte[] variablesArray, int variablesByteSize)
        {
            this.device = device;
            this.resources = resources;
            this.variablesArray = variablesArray;
            this.resourcesCount = resourcesCount;
            this.variablesByteSize = variablesByteSize;
        }

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> with all the captured buffers.
        /// </summary>
        public ReadOnlySpan<DescriptorSetHandle> Resources => this.resources.AsSpan(0, this.resourcesCount);

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> with the padded data representing all the captured variables.
        /// </summary>
        public unsafe ReadOnlySpan<uint> Variables
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref byte r0 = ref MemoryMarshal.GetArrayDataReference(this.variablesArray);
                ref uint r1 = ref Unsafe.As<byte, uint>(ref r0);
                int length = (int)((uint)this.variablesByteSize / sizeof(uint));

                return MemoryMarshal.CreateReadOnlySpan(ref r1, length);
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            foreach (ref readonly var res in this.resources.AsSpan())
            {
                this.device.DisposeDescriptorSet(res);
            }
            ArrayPool<DescriptorSetHandle>.Shared.Return(this.resources);
            ArrayPool<byte>.Shared.Return(this.variablesArray);
        }
    }
}
