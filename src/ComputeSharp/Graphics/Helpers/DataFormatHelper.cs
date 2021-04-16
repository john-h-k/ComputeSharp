using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.Diagnostics;
using Voltium.Core;
using static Voltium.Core.DataFormat;

namespace ComputeSharp.Graphics.Helpers
{
    /// <summary>
    /// A helper type with utility methods for <see cref="DataFormat"/>.
    /// </summary>
    internal static class DataFormatHelper
    {
        public static uint RowSize<T>(uint width)
            where T : unmanaged 
            => (GetForType<T>().BitsPerPixel() * width) / 8;

        public static uint AlignedRowPitch<T>(uint width)
            where T : unmanaged
            => (RowSize<T>(width) + 255u) & ~255u;

        /// <summary>
        /// Gets the appropriate <see cref="DataFormat"/> value for the input type argument.
        /// </summary>
        /// <typeparam name="T">The input type argument to get the corresponding <see cref="DataFormat"/> for.</typeparam>
        /// <returns>The <see cref="DataFormat"/> value corresponding to <typeparamref name="T"/>.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the input type <typeparamref name="T"/> is not supported.</exception>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataFormat GetForType<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(int)) return R32Int;
            else if (typeof(T) == typeof(Int2)) return R32G32Int;
            else if (typeof(T) == typeof(Int3)) return R32G32B32Int;
            else if (typeof(T) == typeof(Int4)) return R32G32B32A32Int;
            else if (typeof(T) == typeof(uint)) return R32UInt;
            else if (typeof(T) == typeof(UInt2)) return R32G32UInt;
            else if (typeof(T) == typeof(UInt3)) return R32G32B32UInt;
            else if (typeof(T) == typeof(UInt4)) return R32G32B32A32UInt;
            else if (typeof(T) == typeof(float)) return R32Single;
            else if (typeof(T) == typeof(Float2)) return R32G32Single;
            else if (typeof(T) == typeof(Float3)) return R32G32B32Single;
            else if (typeof(T) == typeof(Float4)) return R32G32B32A32Single;
            else if (typeof(T) == typeof(Vector2)) return R32G32Single;
            else if (typeof(T) == typeof(Vector3)) return R32G32B32Single;
            else if (typeof(T) == typeof(Vector4)) return R32G32B32A32Single;
            else if (typeof(T) == typeof(Bgra32)) return B8G8R8A8UnsignedNormalized;
            else if (typeof(T) == typeof(Rgba32)) return R8G8B8A8UnsignedNormalized;
            else if (typeof(T) == typeof(Rgba64)) return R16G16B16A16UnsignedNormalized;
            else if (typeof(T) == typeof(R8)) return R8UnsignedNormalized;
            else if (typeof(T) == typeof(R16)) return R16UnsignedNormalized;
            else if (typeof(T) == typeof(Rg16)) return R8G8UnsignedNormalized;
            else if (typeof(T) == typeof(Rg32)) return R16G16UnsignedNormalized;
            else return ThrowHelper.ThrowArgumentException<DataFormat>("Invalid texture type");
        }
    }
}
