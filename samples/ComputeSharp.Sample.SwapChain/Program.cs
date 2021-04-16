using System;
using System.Drawing;
using ComputeSharp.Interop;
using Microsoft.Toolkit.Diagnostics;
using TerraFX.Interop;
using Voltium.Core.Devices;
using FX = TerraFX.Interop.Windows;

namespace ComputeSharp.Sample.SwapChain
{
    class Program
    {
        static void Main(string[] args)
        {
            Win32ApplicationRunner.Run<FractalTilesApplication>();
        }
    }

    internal sealed class FractalTilesApplication : Win32Application
    {
        /// <summary>
        /// The <see cref="ID3D12Device"/> pointer for the device currently in use.
        /// </summary>
        private DXGINativeOutput output;

        /// <summary>
        /// The index of the next buffer that can be used to present content.
        /// </summary>
        private uint currentBufferIndex;

        /// <summary>
        /// The <see cref="ReadWriteTexture2D{T, TPixel}"/> instance used to prepare frames to display.
        /// </summary>
        private ReadWriteTexture2D<Rgba32, Float4> texture = null!;

        public override string Title => "Fractal tiles";

        public override unsafe void OnInitialize(Size size, HWND hwnd)
        {
            // Create the 2D texture to use to generate frames to display
            this.texture = Gpu.Default.AllocateReadWriteTexture2D<Rgba32, Float4>(
                (int)d3D12Resource0Description.Width,
                (int)d3D12Resource0Description.Height);
        }

        public override void OnResize(Size size)
        {
        }

        public override unsafe void OnUpdate(TimeSpan time)
        {
            // Generate the new frame
            //Gpu.Default.For(texture.Width, texture.Height, new FractalTiling(texture, (float)time.TotalSeconds));

            //using ComPtr<ID3D12Resource> d3D12Resource = default;

            // Get the underlying ID3D12Resource pointer for the texture
            //_ = InteropServices.TryGetID3D12Resource(texture, FX.__uuidof<ID3D12Resource>(), (void**)d3D12Resource.GetAddressOf());

            // Get the target back buffer to update
            ID3D12Resource* d3D12ResourceBackBuffer = this.currentBufferIndex switch
            {
                0 => this.d3D12Resource0.Get(),
                1 => this.d3D12Resource1.Get(),
                _ => null
            };

            this.currentBufferIndex ^= 1;

            // Reset the command list to reuse
            this.d3D12GraphicsCommandList.Get()->Reset(this.d3D12CommandAllocator.Get(), null);

            //D3D12_RESOURCE_BARRIER* d3D12ResourceBarriers = stackalloc D3D12_RESOURCE_BARRIER[]
            //{
            //    D3D12_RESOURCE_BARRIER.InitTransition(
            //        d3D12Resource.Get(),
            //        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
            //        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_SOURCE),
            //    D3D12_RESOURCE_BARRIER.InitTransition(
            //        d3D12ResourceBackBuffer,
            //        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
            //        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST)
            //};

            //// Transition the resources to COPY_DEST and COPY_SOURCE respectively
            //d3D12GraphicsCommandList.Get()->ResourceBarrier(2, d3D12ResourceBarriers);

            //// Copy the generated frame to the target back buffer
            //d3D12GraphicsCommandList.Get()->CopyResource(d3D12ResourceBackBuffer, d3D12Resource.Get());

            //d3D12ResourceBarriers[0] = D3D12_RESOURCE_BARRIER.InitTransition(
            //    d3D12Resource.Get(),
            //    D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_SOURCE,
            //    D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

            //d3D12ResourceBarriers[1] = D3D12_RESOURCE_BARRIER.InitTransition(
            //    d3D12ResourceBackBuffer,
            //    D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
            //    D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON);

            //// Transition the resources back to COMMON and UNORDERED_ACCESS respectively
            //d3D12GraphicsCommandList.Get()->ResourceBarrier(2, d3D12ResourceBarriers);

            d3D12GraphicsCommandList.Get()->Close();

            // Execute the command list to perform the copy
            this.d3D12CommandQueue.Get()->ExecuteCommandLists(1, (ID3D12CommandList**)d3D12GraphicsCommandList.GetAddressOf());
            this.d3D12CommandQueue.Get()->Signal(this.d3D12Fence.Get(), this.nextD3D12FenceValue);

            if (this.nextD3D12FenceValue > this.d3D12Fence.Get()->GetCompletedValue())
            {
                this.d3D12Fence.Get()->SetEventOnCompletion(this.nextD3D12FenceValue, default);
            }

            this.nextD3D12FenceValue++;

            // Present the new frame
            //this.dxgiSwapChain1.Get()->Present(0, 0);
        }

        public override void Dispose()
        {
        }
    }

    [AutoConstructor]
    internal readonly partial struct FractalTiling : IComputeShader
    {
        public readonly IReadWriteTexture2D<Float4> texture;
        public readonly float time;

        /// <inheritdoc/>
        public void Execute()
        {
            Float2 position = ((Float2)(256 * ThreadIds.XY)) / texture.Width + time;
            Float4 color = 0;

            for (int i = 0; i < 6; i++)
            {
                Float2 a = Hlsl.Floor(position);
                Float2 b = Hlsl.Frac(position);
                Float4 w = Hlsl.Frac(
                    (Hlsl.Sin(a.X * 7 + 31.0f * a.Y + 0.01f * time) +
                     new Float4(0.035f, 0.01f, 0, 0.7f))
                     * 13.545317f);

                color.XYZ += w.XYZ *
                       2.0f * Hlsl.SmoothStep(0.45f, 0.55f, w.W) *
                       Hlsl.Sqrt(16.0f * b.X * b.Y * (1.0f - b.X) * (1.0f - b.Y));

                position /= 2.0f;
                color /= 2.0f;
            }

            color.XYZ = Hlsl.Pow(color.XYZ, new Float3(0.7f, 0.8f, 0.5f));

            texture[ThreadIds.XY] = color;
        }
    }
}
