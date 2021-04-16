﻿using System.Diagnostics;
using ComputeSharp.__Internals;
using ComputeSharp.Exceptions;
using ComputeSharp.Graphics.Resources.Enums;
using ComputeSharp.Resources;
using ComputeSharp.Resources.Debug;
using Voltium.Core.Devices;
using ResourceType = ComputeSharp.Graphics.Resources.Enums.ResourceType;

#pragma warning disable CS0618

namespace ComputeSharp
{
    /// <summary>
    /// A <see langword="class"/> representing a typed readonly 2D texture stored on GPU memory.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the texture.</typeparam>
    /// <typeparam name="TPixel">The type of pixels used on the GPU side.</typeparam>
    [DebuggerTypeProxy(typeof(Texture2DDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public sealed class ReadOnlyTexture2D<T, TPixel> : Texture2D<T>, IReadOnlyTexture2D<TPixel>
        where T : unmanaged, IUnorm<TPixel>
        where TPixel : unmanaged
    {
        /// <summary>
        /// Creates a new <see cref="ReadOnlyTexture2D{T,TPixel}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> associated with the current instance.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="allocationMode">The allocation mode to use for the new resource.</param>
        internal ReadOnlyTexture2D(GraphicsDevice device, int width, int height, AllocationMode allocationMode)
            : base(device, width, height, ResourceType.ReadOnly, allocationMode, FormatSupport.Tex2D)
        {
        }

        /// <inheritdoc/>
        public TPixel this[int x, int y] => throw new InvalidExecutionContextException($"{nameof(ReadOnlyTexture2D<T, TPixel>)}<T>[int,int]");

        /// <inheritdoc/>
        public TPixel this[Int2 xy] => throw new InvalidExecutionContextException($"{nameof(ReadOnlyTexture2D<T,TPixel>)}<T>[Int2]");

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ComputeSharp.ReadOnlyTexture2D<{typeof(T)},{typeof(TPixel)}>[{Width}, {Height}]";
        }
    }
}
