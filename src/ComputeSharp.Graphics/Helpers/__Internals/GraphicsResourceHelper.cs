using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ComputeSharp.Resources;
using Microsoft.Toolkit.Diagnostics;
using Voltium.Core.NativeApi;

namespace ComputeSharp.__Internals
{
    /// <summary>
    /// A helper class with some proxy methods to expose to generated code in external projects.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is not intended to be used directly by user code")]
    public static class GraphicsResourceHelper
    {
        /// <summary>
        /// An interface for non-generic graphics resource types.
        /// </summary>
        internal interface IGraphicsResource
        {
            /// <summary>
            /// Validates the given resource for usage with a specified device, and retrieves its GPU descriptor handle.
            /// </summary>
            /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
            /// <returns>The GPU descriptor handle for the resource.</returns> 
            DescriptorSetHandle ValidateAndGetDescriptorSetHandle(GraphicsDevice device);
        }

        /// <summary>
        /// Validates the given buffer for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="T">The type of values stored in the input buffer.</typeparam>
        /// <param name="buffer">The input <see cref="Buffer{T}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the buffer.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DescriptorSetHandle ValidateAndGetDescriptorSetHandle<T>(Buffer<T> buffer, GraphicsDevice device)
            where T : unmanaged
        {
            buffer.ThrowIfDisposed();
            buffer.ThrowIfDeviceMismatch(device);

            return buffer.Descriptor;
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="T">The type of values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="ReadOnlyTexture2D{T}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DescriptorSetHandle ValidateAndGetDescriptorSetHandle<T>(ReadOnlyTexture2D<T> texture, GraphicsDevice device)
            where T : unmanaged
        {
            texture.ThrowIfDisposed();
            texture.ThrowIfDeviceMismatch(device);

            return texture.Descriptor;
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="T">The type of values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="ReadWriteTexture2D{T}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DescriptorSetHandle ValidateAndGetDescriptorSetHandle<T>(ReadWriteTexture2D<T> texture, GraphicsDevice device)
            where T : unmanaged
        {
            texture.ThrowIfDisposed();
            texture.ThrowIfDeviceMismatch(device);

            return texture.Descriptor;
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="TPixel">The type of normalized values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="IReadOnlyTexture2D{TPixel}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DescriptorSetHandle ValidateAndGetDescriptorSetHandle<TPixel>(IReadOnlyTexture2D<TPixel> texture, GraphicsDevice device)
            where TPixel : unmanaged
        {
            if (texture is IGraphicsResource resource)
            {
                return resource.ValidateAndGetDescriptorSetHandle(device);
            }

            return ThrowHelper.ThrowArgumentException<DescriptorSetHandle>("The input texture is not a valid instance");
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="TPixel">The type of normalized values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="IReadWriteTexture2D{TPixel}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DescriptorSetHandle ValidateAndGetDescriptorSetHandle<TPixel>(IReadWriteTexture2D<TPixel> texture, GraphicsDevice device)
            where TPixel : unmanaged
        {
            if (texture is IGraphicsResource resource)
            {
                return resource.ValidateAndGetDescriptorSetHandle(device);
            }

            return ThrowHelper.ThrowArgumentException<DescriptorSetHandle>("The input texture is not a valid instance");
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="T">The type of values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="ReadOnlyTexture3D{T}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DescriptorSetHandle ValidateAndGetDescriptorSetHandle<T>(ReadOnlyTexture3D<T> texture, GraphicsDevice device)
            where T : unmanaged
        {
            texture.ThrowIfDisposed();
            texture.ThrowIfDeviceMismatch(device);

            return texture.Descriptor;
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="T">The type of values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="ReadWriteTexture3D{T}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DescriptorSetHandle ValidateAndGetDescriptorSetHandle<T>(ReadWriteTexture3D<T> texture, GraphicsDevice device)
            where T : unmanaged
        {
            texture.ThrowIfDisposed();
            texture.ThrowIfDeviceMismatch(device);

            return texture.Descriptor;
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="TPixel">The type of normalized values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="IReadOnlyTexture3D{TPixel}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DescriptorSetHandle ValidateAndGetDescriptorSetHandle<TPixel>(IReadOnlyTexture3D<TPixel> texture, GraphicsDevice device)
            where TPixel : unmanaged
        {
            if (texture is IGraphicsResource resource)
            {
                return resource.ValidateAndGetDescriptorSetHandle(device);
            }

            return ThrowHelper.ThrowArgumentException<DescriptorSetHandle>("The input texture is not a valid instance");
        }

        /// <summary>
        /// Validates the given texture for usage with a specified device, and retrieves its GPU descriptor handle.
        /// </summary>
        /// <typeparam name="TPixel">The type of normalized values stored in the input texture.</typeparam>
        /// <param name="texture">The input <see cref="IReadWriteTexture3D{TPixel}"/> instance to check.</param>
        /// <param name="device">The target <see cref="GraphicsDevice"/> instance in use.</param>
        /// <returns>The GPU descriptor handle for the texture.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DescriptorSetHandle ValidateAndGetDescriptorSetHandle<TPixel>(IReadWriteTexture3D<TPixel> texture, GraphicsDevice device)
            where TPixel : unmanaged
        {
            if (texture is IGraphicsResource resource)
            {
                return resource.ValidateAndGetDescriptorSetHandle(device);
            }

            return ThrowHelper.ThrowArgumentException<DescriptorSetHandle>("The input texture is not a valid instance");
        }
    }
}
