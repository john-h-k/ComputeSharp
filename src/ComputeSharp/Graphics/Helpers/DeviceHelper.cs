using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using ComputeSharp.Core.Extensions;
using Microsoft.Toolkit.Diagnostics;
using TerraFX.Interop;
using HRESULT = System.Int32;
using FX = TerraFX.Interop.Windows;

namespace ComputeSharp.Graphics.Helpers
{
    /// <summary>
    /// A <see langword="class"/> with methods to inspect the available devices on the current machine.
    /// </summary>
    internal static partial class DeviceHelper
    {
        /// <summary>
        /// The vendor id for Microsoft adapters (the "Microsoft Basic Render").
        /// </summary>
        private const uint MicrosoftVendorId = 0x1414;

        /// <summary>
        /// The device id for the WARP device.
        /// </summary>
        private const uint WarpDeviceId = 0x8C;

        /// <summary>
        /// The <see cref="Lazy{T}"/> instance used to produce the default <see cref="GraphicsDevice"/> instance.
        /// </summary>
        public static readonly Lazy<GraphicsDevice> DefaultFactory = new(GetDefaultDevice);

        /// <summary>
        /// Gets the <see cref="Luid"/> of the default device.
        /// </summary>
        /// <returns>The <see cref="Luid"/> of the default device supporting <see cref="D3D_FEATURE_LEVEL_11_0"/> and <see cref="D3D_SHADER_MODEL_6_0"/>.</returns>
        /// <remarks>This methods assumes that a default device is available.</remarks>
        [Pure]
        public static unsafe Luid GetDefaultDeviceLuid()
        {
            if (DefaultFactory.IsValueCreated)
            {
                return DefaultFactory.Value.Luid;
            }

            DXGI_ADAPTER_DESC1 dxgiDescription1;

            _ = TryGetDefaultDevice(null, &dxgiDescription1) || TryGetWarpDevice(null, &dxgiDescription1);

            return Luid.FromLUID(dxgiDescription1.AdapterLuid);
        }

        /// <summary>
        /// Gets the default <see cref="GraphicsDevice"/> instance.
        /// </summary>
        /// <returns>The default <see cref="GraphicsDevice"/> instance supporting <see cref="D3D_FEATURE_LEVEL_11_0"/> and <see cref="D3D_SHADER_MODEL_6_0"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown when a default device is not available.</exception>
        private static unsafe GraphicsDevice GetDefaultDevice()
        {
            using ComPtr<IDXGIAdapter1> dxgiAdapter = default;

            DXGI_ADAPTER_DESC1 dxgiDescription1;

            if (TryGetDefaultDevice(&dxgiAdapter, &dxgiDescription1) ||
                TryGetWarpDevice(&dxgiAdapter, &dxgiDescription1))
            {
                return GetOrCreateDevice(dxgiAdapter.Get(), &dxgiDescription1);
            }

            return ThrowHelper.ThrowNotSupportedException<GraphicsDevice>("Failed to retrieve the default device.");
        }

        /// <summary>
        /// Tries to check or create a default <see cref="ID3D12Device"/> object.
        /// </summary>
        /// <param name="pAdapter">A pointer to the <see cref="ID3D12Device"/> object to create, or <see langword="null"/>.</param>
        /// <param name="dxgiDescription1">A pointer to the <see cref="DXGI_ADAPTER_DESC1"/> value for the device found.</param>
        /// <returns>Whether a default device was found with the requested feature level.</returns>
        private static unsafe bool TryGetDefaultDevice(ComPtr<IDXGIAdapter1>* pAdapter, DXGI_ADAPTER_DESC1* dxgiDescription1)
        {
            using ComPtr<IDXGIFactory4> dxgiFactory4 = default;

            EnableDebugMode();

            FX.CreateDXGIFactory2(IDXGIFactoryCreationFlags, FX.__uuidof<IDXGIFactory4>(), dxgiFactory4.GetVoidAddressOf()).Assert();

            uint i = 0;

            while (true)
            {
                using ComPtr<IDXGIAdapter1> dxgiAdapter1 = default;

                HRESULT enumAdapters1Result = dxgiFactory4.Get()->EnumAdapters1(i++, dxgiAdapter1.GetAddressOf());

                if (enumAdapters1Result == FX.DXGI_ERROR_NOT_FOUND)
                {
                    return false;
                }

                enumAdapters1Result.Assert();

                dxgiAdapter1.Get()->GetDesc1(dxgiDescription1).Assert();

                if (dxgiDescription1->DedicatedVideoMemory == 0) continue;

                dxgiAdapter1.Swap(pAdapter);
                return true;
            }
        }

        /// <summary>
        /// Tries to check or create a warp <see cref="ID3D12Device"/> object.
        /// </summary>
        /// <param name="d3D12Device">A pointer to the <see cref="ID3D12Device"/> object to create, or <see langword="null"/>.</param>
        /// <param name="dxgiAdapter">A pointer to the <see cref="IDXGIAdapter"/> object used to create <paramref name="d3D12Device"/>, or <see langword="null"/>.</param>
        /// <param name="dxgiDescription1">A pointer to the <see cref="DXGI_ADAPTER_DESC1"/> value for the device found.</param>
        /// <returns>Whether a warp device was created successfully.</returns>
        private static unsafe bool TryGetWarpDevice(ComPtr<IDXGIAdapter1>* pAdapter, DXGI_ADAPTER_DESC1* dxgiDescription1)
        {
            using ComPtr<IDXGIFactory4> dxgiFactory4 = default;

            EnableDebugMode();

            FX.CreateDXGIFactory2(IDXGIFactoryCreationFlags, FX.__uuidof<IDXGIFactory4>(), dxgiFactory4.GetVoidAddressOf()).Assert();

            using ComPtr<IDXGIAdapter1> dxgiAdapter1 = default;

            dxgiFactory4.Get()->EnumWarpAdapter(FX.__uuidof<IDXGIAdapter1>(), dxgiAdapter1.GetVoidAddressOf()).Assert();
            
            dxgiAdapter1.Get()->GetDesc1(dxgiDescription1).Assert();

            dxgiAdapter1.Swap(pAdapter);

            return true;
        }

        /// <summary>
        /// The creation flags for <see cref="IDXGIFactory"/> instances.
        /// </summary>
        private const uint IDXGIFactoryCreationFlags =
#if DEBUG
            FX.DXGI_CREATE_FACTORY_DEBUG;
#else
            0;
#endif

        /// <summary>
        /// Enables the debug layer for DirectX APIs.
        /// </summary>
        [Conditional("DEBUG")]
        private static unsafe void EnableDebugMode()
        {
            using ComPtr<ID3D12Debug> d3D12Debug = default;
            using ComPtr<ID3D12Debug1> d3D12Debug1 = default;

            FX.D3D12GetDebugInterface(FX.__uuidof<ID3D12Debug>(), d3D12Debug.GetVoidAddressOf()).Assert();

            d3D12Debug.Get()->EnableDebugLayer();

            if (FX.SUCCEEDED(d3D12Debug.CopyTo(d3D12Debug1.GetAddressOf())))
            {
                d3D12Debug1.Get()->SetEnableGPUBasedValidation(FX.TRUE);
                d3D12Debug1.Get()->SetEnableSynchronizedCommandQueueValidation(FX.TRUE);
            }
        }
    }
}
