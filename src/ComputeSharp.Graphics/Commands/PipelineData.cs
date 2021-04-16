using ComputeSharp.Interop;
using Voltium.Core.Devices;
using Voltium.Core.NativeApi;

namespace ComputeSharp.Graphics.Commands
{
    /// <summary>
    /// A <see langword="class"/> representing a custom pipeline state for a compute operation.
    /// </summary>
    internal sealed unsafe class PipelineData : NativeObject
    {
        /// <summary>
        /// The <see cref="PipelineHandle"/> instance for the current <see cref="PipelineData"/> object.
        /// </summary>
        private PipelineHandle pipeline;

        /// <summary>
        /// Creates a new <see cref="PipelineData"/> instance with the specified parameters.
        /// </summary>
        /// <param name="d3D12RootSignature">The <see cref="ID3D12RootSignature"/> value for the current shader.</param>
        /// <param name="d3D12PipelineState">The compiled pipeline state to reuse for the current shader.</param>
        internal PipelineData(INativeDevice device, PipelineHandle pipeline) : base(device)
        {
            this.pipeline = pipeline;
        }

        /// <summary>
        /// Gets the <see cref="PipelineHandle"/> instance for the current <see cref="PipelineData"/> object.
        /// </summary>
        public PipelineHandle Pipeline => this.pipeline;

        /// <inheritdoc/>
        protected override bool OnDispose()
        {
            this.device.DisposePipeline(this.pipeline);

            return true;
        }
    }
}
