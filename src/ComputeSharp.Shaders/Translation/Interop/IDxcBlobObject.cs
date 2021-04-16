using ComputeSharp.Interop;
using TerraFX.Interop;
using Voltium.Core.Devices;

namespace ComputeSharp.Shaders.Translation.Interop
{
    /// <summary>
    /// An object wrapping an <see cref="IDxcBlob"/> instance.
    /// </summary>
    internal sealed unsafe class IDxcBlobObject : NativeObject
    {
        /// <summary>
        /// The <see cref="IDxcBlob"/> instance currently in use.
        /// </summary>
        private ComPtr<IDxcBlob> dxcBlob;

        /// <summary>
        /// Creates a new <see cref="IDxcBlobObject"/> instance with the specified parameters.
        /// </summary>
        /// <param name="dxcBlob">The <see cref="IDxcBlob"/> instance to wrap.</param>
        public IDxcBlobObject(IDxcBlob* dxcBlob)
        {
            this.dxcBlob = dxcBlob;
            this.ShaderBytecode = new CompiledShader(dxcBlob->GetBufferPointer(), (nint)dxcBlob->GetBufferSize(), ShaderType.Compute);
        }

        /// <summary>
        /// Gets a raw pointer to the <see cref="IDxcBlob"/> instance in use.
        /// </summary>
        public CompiledShader ShaderBytecode { get; }

        /// <inheritdoc/>
        protected override bool OnDispose()
        {
            this.dxcBlob.Dispose();

            return true;
        }
    }
}
