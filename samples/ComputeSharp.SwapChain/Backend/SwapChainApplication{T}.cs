using System;
using System.Runtime.InteropServices;
using ComputeSharp.Graphics.Commands;
using ComputeSharp.Interop;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Devices;
using FX = TerraFX.Interop.Windows;

namespace ComputeSharp.SwapChain.Backend
{
    internal sealed class SwapChainApplication<T> : Win32Application
        where T : struct, IComputeShader
    {
        private D3D12NativeQueue queue = null!;

        /// <summary>
        /// The <see cref="Func{T1, T2, TResult}"/> instance used to create shaders to run.
        /// </summary>
        private readonly Func<IReadWriteTexture2D<Float4>, TimeSpan, T> shaderFactory;

        /// <summary>
        /// The <see cref="ID3D12Device"/> pointer for the device currently in use.
        /// </summary>
        private DXGINativeOutput output = null!;

        /// <summary>
        /// The <see cref="ReadWriteTexture2D{T, TPixel}"/> instance used to prepare frames to display.
        /// </summary>
        private ReadWriteTexture2D<Rgba32, Float4>? texture;

        /// <summary>
        /// Whether or not the window has been resized and requires the buffers to be updated.
        /// </summary>
        private bool isResizePending;

        /// <summary>
        /// Creates a new <see cref="SwapChainApplication"/> instance with the specified parameters.
        /// </summary>
        /// <param name="shaderFactory">The <see cref="Func{T1, T2, TResult}"/> instance used to create shaders to run.</param>
        public SwapChainApplication(Func<IReadWriteTexture2D<Float4>, TimeSpan, T> shaderFactory)
        {
            this.shaderFactory = shaderFactory;

            var device = (D3D12NativeDevice)InteropServices.GetNativeDevice(Gpu.Default);
            this.queue = (D3D12NativeQueue)device.CreateQueue(ExecutionEngine.Graphics);
        }

        /// <inheritdoc/>
        public override unsafe void OnInitialize(HWND hwnd)
        {
            this.output = new DXGINativeOutput(
                this.queue,
                new NativeOutputDesc
                { 
                    BackBufferCount = 2,
                    Format = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                    PreserveBackBuffers = false,
                    VrStereo = false
                },
                hwnd
            );
        }

        /// <inheritdoc/>
        public override unsafe void OnResize()
        {
            this.isResizePending = true;
        }

        /// <summary>
        /// Applies the actual resize logic that was scheduled from <see cref="OnResize"/>.
        /// </summary>
        private unsafe void ApplyResize()
        {
            this.output.Resize(0, 0);


            // Create the 2D texture to use to generate frames to display
            this.texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(
                (int)this.output.Width,
                (int)this.output.Height);
        }

        /// <inheritdoc/>
        public override unsafe void OnUpdate(TimeSpan time)
        {
            if (this.isResizePending)
            {
                ApplyResize();

                this.isResizePending = false;
            }

            // Generate the new frame
            Gpu.Default.For(this.texture!.Width, this.texture.Height, this.shaderFactory(this.texture, time));


            var commandList = CommandList.Create();

            commandList.ResourceTransition(this.output.BackBuffer, ResourceState.Present, ResourceState.CopyDestination);
            commandList.CopyTexture(InteropServices.GetUnderlyingHandle(this.texture), this.output.BackBuffer, 0);
            commandList.ResourceTransition(this.output.BackBuffer, ResourceState.CopyDestination, ResourceState.Present);

            var buff = commandList.Buffer;
            var task = queue.Execute(MemoryMarshal.CreateSpan(ref buff, 1), default);
            this.output.Present();
            task.Block();
        }
    }
}
