﻿using System;
using System.Runtime.CompilerServices;
using ComputeSharp.Core.Extensions;
using ComputeSharp.Shaders.Renderer;
using ComputeSharp.Shaders.Translation;
using TerraFX.Interop;
using FX = TerraFX.Interop.Windows;

namespace ComputeSharp.Interop
{
    /// <summary>
    /// Provides methods to extract reflection info on compute shaders generated using this library.
    /// </summary>
    public static class ReflectionServices
    {
        /// <summary>
        /// Gets the shader info associated with a given shader.
        /// <para>
        /// This overload can be used for simplicity when the shader being inspected does not rely on captured
        /// objects to be processed correctly. This is the case when it does not contain any <see cref="Delegate"/>-s.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of shader to retrieve info for.</typeparam>
        /// <param name="shaderInfo">The resulting <see cref="ShaderInfo"/> instance.</param>
        /// <remarks>
        /// The thread group sizes will always be set to (1, 1, 1) in the returned shader. This is done to
        /// avoid having to compiler multiple shaders just to get reflection info for them. When using any of
        /// the APIs to dispatch a shader, the thread sizes would actually be set to a proper value insead.
        /// </remarks>
        public static void GetShaderInfo<T>(out ShaderInfo shaderInfo)
            where T : struct, IComputeShader
        {
            GetShaderInfo(default(T), out shaderInfo);
        }

        /// <summary>
        /// Gets the shader info associated with a given shader.
        /// </summary>
        /// <typeparam name="T">The type of shader to retrieve info for.</typeparam>
        /// <param name="shader">The input shader to retrieve info for.</param>
        /// <param name="shaderInfo">The resulting <see cref="ShaderInfo"/> instance.</param>
        /// <remarks>
        /// The thread group sizes will always be set to (1, 1, 1) in the returned shader. This is done to
        /// avoid having to compiler multiple shaders just to get reflection info for them. When using any of
        /// the APIs to dispatch a shader, the thread sizes would actually be set to a proper value insead.
        /// </remarks>
        public static unsafe void GetShaderInfo<T>(in T shader, out ShaderInfo shaderInfo)
            where T : struct, IComputeShader
        {
            ShaderLoader<T> shaderLoader = ShaderLoader<T>.Load(in shader);

            using var shaderSource = HlslShaderRenderer.Render(1, 1, 1, shaderLoader);

            using ComPtr<IDxcBlob> dxcBlobBytecode = ShaderCompiler.Instance.CompileShader(shaderSource.WrittenSpan);

            using ComPtr<IDxcUtils> dxcUtils = default;
            Guid dxcLibraryClsid = FX.CLSID_DxcLibrary;

            FX.DxcCreateInstance(&dxcLibraryClsid, FX.__uuidof<IDxcUtils>(), dxcUtils.GetVoidAddressOf()).Assert();

            using ComPtr<ID3D12ShaderReflection> d3D12ShaderReflection = default;

            DxcBuffer dxcBuffer = default;
            dxcBuffer.Ptr = dxcBlobBytecode.Get()->GetBufferPointer();
            dxcBuffer.Size = dxcBlobBytecode.Get()->GetBufferSize();

            dxcUtils.Get()->CreateReflection(
                &dxcBuffer,
                FX.__uuidof<ID3D12ShaderReflection>(),
                d3D12ShaderReflection.GetVoidAddressOf()).Assert();

            D3D12_SHADER_DESC d3D12ShaderDescription;

            d3D12ShaderReflection.Get()->GetDesc(&d3D12ShaderDescription).Assert();

            shaderInfo = default;
            Unsafe.AsRef(shaderInfo.CompilerVersion) = new string(d3D12ShaderDescription.Creator);
            Unsafe.AsRef(shaderInfo.HlslSource) = shaderSource.WrittenSpan.ToString();
            Unsafe.AsRef(shaderInfo.ConstantBufferCount) = d3D12ShaderDescription.ConstantBuffers;
            Unsafe.AsRef(shaderInfo.BoundResourceCount) = d3D12ShaderDescription.BoundResources;
            Unsafe.AsRef(shaderInfo.InstructionCount) = d3D12ShaderDescription.InstructionCount;
            Unsafe.AsRef(shaderInfo.TemporaryRegisterCount) = d3D12ShaderDescription.TempRegisterCount;
            Unsafe.AsRef(shaderInfo.TemporaryArrayCount) = d3D12ShaderDescription.TempArrayCount;
            Unsafe.AsRef(shaderInfo.ConstantDefineCount) = d3D12ShaderDescription.DefCount;
            Unsafe.AsRef(shaderInfo.DeclarationCount) = d3D12ShaderDescription.DclCount;
            Unsafe.AsRef(shaderInfo.TextureNormalInstructions) = d3D12ShaderDescription.TextureNormalInstructions;
            Unsafe.AsRef(shaderInfo.TextureLoadInstructionCount) = d3D12ShaderDescription.TextureLoadInstructions;
            Unsafe.AsRef(shaderInfo.TextureStoreInstructionCount) = d3D12ShaderDescription.cTextureStoreInstructions;
            Unsafe.AsRef(shaderInfo.FloatInstructionCount) = d3D12ShaderDescription.FloatInstructionCount;
            Unsafe.AsRef(shaderInfo.IntInstructionCount) = d3D12ShaderDescription.IntInstructionCount;
            Unsafe.AsRef(shaderInfo.UIntInstructionCount) = d3D12ShaderDescription.UintInstructionCount;
            Unsafe.AsRef(shaderInfo.StaticFlowControlInstructionCount) = d3D12ShaderDescription.StaticFlowControlCount;
            Unsafe.AsRef(shaderInfo.DynamicFlowControlInstructionCount) = d3D12ShaderDescription.DynamicFlowControlCount;
            Unsafe.AsRef(shaderInfo.EmitInstructionCount) = d3D12ShaderDescription.EmitInstructionCount;
            Unsafe.AsRef(shaderInfo.BarrierInstructionCount) = d3D12ShaderDescription.cBarrierInstructions;
            Unsafe.AsRef(shaderInfo.InterlockedInstructionCount) = d3D12ShaderDescription.cInterlockedInstructions;
            Unsafe.AsRef(shaderInfo.BitwiseInstructionCount) = d3D12ShaderReflection.Get()->GetBitwiseInstructionCount();
            Unsafe.AsRef(shaderInfo.MovcInstructionCount) = d3D12ShaderReflection.Get()->GetMovcInstructionCount();
            Unsafe.AsRef(shaderInfo.MovInstructionCount) = d3D12ShaderReflection.Get()->GetMovInstructionCount();
            Unsafe.AsRef(shaderInfo.InterfaceSlotCount) = d3D12ShaderReflection.Get()->GetNumInterfaceSlots();
        }
    }
}