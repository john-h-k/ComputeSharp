﻿using System.Diagnostics;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Resources.Enums;
using ComputeSharp.Resources;
using ComputeSharp.Resources.Debug;
using Voltium.Core.Devices;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;


namespace ComputeSharp
{
    /// <summary>
    /// A <see langword="class"/> representing a typed read write 2D texture stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the texture.</typeparam>
    [DebuggerTypeProxy(typeof(Texture2DDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public sealed class ReadWriteTexture2D<T> : Texture2D<T>
        where T : unmanaged
    {
        /// <summary>
        /// Creates a new <see cref="ReadWriteTexture2D{T}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        internal ReadWriteTexture2D(GraphicsDevice device, int width, int height, AllocationMode allocationMode)
            : base(device, width, height, ResourceType.ReadWrite, allocationMode, FormatSupport.Tex2D | FormatSupport.TypedUnorderedAccess)
        {
        }

        /// <summary>
        /// Gets a single <typeparamref name="T"/> value from the current writeable texture.
        /// </summary>
        /// <param name="x">The horizontal offset of the value to get.</param>
        /// <param name="y">The vertical offset of the value to get.</param>
        /// <remarks>This API can only be used from a compute shader, and will always throw if used anywhere else.</remarks>
        public ref T this[int x, int y] => throw new InvalidExecutionContextException($"{typeof(ReadWriteTexture2D<T>)}[{typeof(int)}, {typeof(int)}]");

        /// <summary>
        /// Gets or sets a single <typeparamref name="T"/> value from the current writeable texture.
        /// </summary>
        /// <param name="xy">The coordinates of the value to get.</param>
        /// <remarks>This API can only be used from a compute shader, and will always throw if used anywhere else.</remarks>
        public ref T this[Int2 xy] => throw new InvalidExecutionContextException($"{typeof(ReadWriteTexture2D<T>)}[{typeof(Int2)}]");

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ComputeSharp.ReadWriteTexture2D<{typeof(T)}>[{Width}, {Height}]";
        }
    }
}
