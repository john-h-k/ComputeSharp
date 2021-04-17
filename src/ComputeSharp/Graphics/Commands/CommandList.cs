using ComputeSharp.Resources;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.NativeApi;

namespace ComputeSharp.Graphics.Commands
{
    public unsafe struct CommandList
    {
        private ArrayBufferWriter<byte> _buffer;
        private PipelineHandle _first;

        public static CommandList Create()
            => new CommandList() { _buffer = new ArrayBufferWriter<byte>(64) };


        public void CopyTexture(TextureHandle source, TextureHandle dest, uint subresource)
        {
            ref var cmd = ref Allocate<CommandTextureCopy>();

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.SourceSubresource = subresource;
            cmd.DestSubresource = subresource;

            Advance<CommandTextureCopy>();
        }

        public void CopyBuffer(BufferHandle source, BufferHandle dest, uint length)
            => CopyBuffer(source, 0, dest, 0, length);
        public void CopyBuffer(BufferHandle source, uint srcOffset, BufferHandle dest, uint destOffset, uint length)
        {
            ref var cmd = ref Allocate<CommandBufferCopy>();

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.SourceOffset = srcOffset;
            cmd.DestOffset = destOffset;
            cmd.Length = length;

            Advance<CommandBufferCopy>();
        }

        public void ResourceTransition(TextureHandle texture, ResourceState before, ResourceState after)
        {
            ref var cmd = ref Allocate<CommandTransitions, ResourceTransitionBarrier>();
            ref var barrier = ref Unsafe.As<CommandTransitions, ResourceTransitionBarrier>(ref Unsafe.Add(ref cmd, 1));

            cmd.Count = 1;
            barrier.Resource = texture;
            barrier.Subresource = uint.MaxValue;
            barrier.Before = before;
            barrier.After = after;

            Advance<CommandTransitions, ResourceTransitionBarrier>();
        }

        public void CopyBufferToTexture(
            DataFormat format,
            BufferHandle source,
            uint offset,
            TextureHandle dest,
            in TextureFootprint footprint,
            uint subresource,
            uint destX,
            uint destY,
            uint destZ,
            uint sourceX,
            uint sourceY,
            uint sourceZ)
        {
            ref var cmd = ref Allocate<CommandBufferToTextureCopy, Box>();
            ref var box = ref Unsafe.As<CommandBufferToTextureCopy, Box>(ref Unsafe.Add(ref cmd, 1));

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.SourceOffset = offset;
            cmd.SourceFormat = format;
            cmd.DestSubresource = subresource;
            cmd.SourceWidth = footprint.Width;
            cmd.SourceHeight = footprint.Height;
            cmd.SourceDepth = footprint.Depth;
            cmd.SourceRowPitch = footprint.RowPitch;
            cmd.DestX = destX;
            cmd.DestY = destY;
            cmd.DestZ = destZ;
            cmd.HasBox = true;

            box.Left = sourceX;
            box.Top = sourceY;
            box.Front = sourceZ;
            box.Right = sourceX + footprint.Width;
            box.Bottom = sourceY + footprint.Height;
            box.Back = sourceZ + footprint.Depth;

            Advance<CommandBufferToTextureCopy, Box>();
        }

        public void CopyBufferToTexture(
            DataFormat format,
            BufferHandle source,
            uint offset,
            TextureHandle dest, 
            in TextureFootprint footprint, 
            uint subresource,
            uint destX = 0,
            uint destY = 0,
            uint destZ = 0)
        {
            ref var cmd = ref Allocate<CommandBufferToTextureCopy>();

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.SourceOffset = offset;
            cmd.SourceFormat = format;
            cmd.DestSubresource = subresource;
            cmd.SourceWidth = footprint.Width;
            cmd.SourceHeight = footprint.Height;
            cmd.SourceDepth = footprint.Depth;
            cmd.SourceRowPitch = footprint.RowPitch;
            cmd.DestX = destX;
            cmd.DestY = destY;
            cmd.DestZ = destZ;

            Advance<CommandBufferToTextureCopy>();
        }

        public void CopyTextureToBuffer(
           DataFormat format,
           TextureHandle source, 
           uint subresource,
           BufferHandle dest,
           uint offset,
           in TextureFootprint footprint,
           uint destX,
           uint destY,
           uint destZ,
           uint sourceX,
           uint sourceY,
           uint sourceZ)
        {
            ref var cmd = ref Allocate<CommandTextureToBufferCopy, Box>();
            ref var box = ref Unsafe.As<CommandTextureToBufferCopy, Box>(ref Unsafe.Add(ref cmd, 1));

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.DestOffset = offset;
            cmd.SourceFormat = format;
            cmd.SourceSubresource = subresource;
            cmd.DestWidth = footprint.Width;
            cmd.DestHeight = footprint.Height;
            cmd.DestDepth = footprint.Depth;
            cmd.DestRowPitch = footprint.RowPitch;
            cmd.DestX = destX;
            cmd.DestY = destY;
            cmd.DestZ = destZ;
            cmd.HasBox = true;

            box.Left = sourceX;
            box.Top = sourceY;
            box.Front = sourceZ;
            box.Right = sourceX + footprint.Width;
            box.Bottom = sourceY + footprint.Height;
            box.Back = sourceZ + footprint.Depth;

            Advance<CommandTextureToBufferCopy, Box>();
        }

        public void CopyTextureToBuffer(
           DataFormat format,
           TextureHandle source,
           uint subresource,
           BufferHandle dest,
           uint offset,
           in TextureFootprint footprint,
           uint destX = 0,
           uint destY = 0,
           uint destZ = 0)
        {
            ref var cmd = ref Allocate<CommandTextureToBufferCopy>();

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.DestOffset = offset;
            cmd.SourceFormat = format;
            cmd.SourceSubresource = subresource;
            cmd.DestWidth = footprint.Width;
            cmd.DestHeight = footprint.Height;
            cmd.DestDepth = footprint.Depth;
            cmd.DestRowPitch = footprint.RowPitch;
            cmd.DestX = destX;
            cmd.DestY = destY;
            cmd.DestZ = destZ;

            Advance<CommandTextureToBufferCopy>();
        }

        public void SetPipeline(PipelineHandle handle)
        {
            if (_first == default)
            {
                _first = handle;
            }

            ref var cmd = ref Allocate<CommandSetPipeline>();

            cmd.Pipeline = handle;

            Advance<CommandSetPipeline>();
        }

        public void SetConstants(uint paramIndex, ReadOnlySpan<uint> data)
        {
            ref var cmd = ref Allocate<CommandBind32BitConstants, uint>(data.Length, out var size);
            ref var constants = ref Unsafe.As<CommandBind32BitConstants, uint>(ref Unsafe.Add(ref cmd, 1));

            cmd.BindPoint = BindPoint.Compute;
            cmd.ParameterIndex = paramIndex;
            cmd.Num32BitValues = (uint)data.Length;

            data.CopyTo(MemoryMarshal.CreateSpan(ref constants, data.Length));

            _buffer.Advance(size);
        }

        public void SetResources(uint paramIndex, ReadOnlySpan<DescriptorSetHandle> resources)
        {
            ref var cmd = ref Allocate<CommandBindDescriptors, DescriptorSetHandle>(resources.Length, out var size);
            ref var descriptors = ref Unsafe.As<CommandBindDescriptors, DescriptorSetHandle>(ref Unsafe.Add(ref cmd, 1));

            cmd.BindPoint = BindPoint.Compute;
            cmd.FirstSetIndex = paramIndex;
            cmd.SetCount = (uint)resources.Length;

            resources.CopyTo(MemoryMarshal.CreateSpan(ref descriptors, resources.Length));

            _buffer.Advance(size);
        }

        public void Dispatch(uint x, uint y, uint z)
        {
            ref var cmd = ref Allocate<CommandDispatch>();

            cmd.X = x;
            cmd.Y = y;
            cmd.Z = z;

            Advance<CommandDispatch>();
        }

        private int Align(int size) => MathHelpers.AlignUp(size, sizeof(CommandType));

        private void Advance<TCommand>() 
            where TCommand : unmanaged 
            => _buffer.Advance(Align(sizeof(CommandType) + sizeof(TCommand)));
        private void Advance<TCommand, TVariable>() 
            where TCommand : unmanaged 
            where TVariable : unmanaged 
            => _buffer.Advance(Align(sizeof(CommandType) + sizeof(TCommand) + sizeof(TVariable)));
        private void Advance(int count) => _buffer.Advance(count);
        private void Advance<TCommand, TVariable>(int count)
            where TCommand : unmanaged
            where TVariable : unmanaged
            => _buffer.Advance(Align(sizeof(CommandType) + sizeof(TCommand) + sizeof(TVariable) * count));

        private ref TCommand Allocate<TCommand>() where TCommand : unmanaged, ICommand
        {
            var advanceSize = Align(sizeof(CommandType) + sizeof(TCommand));
            var data = _buffer.GetSpan(advanceSize);

            ref var start = ref MemoryMarshal.GetReference(data);
            Unsafe.As<byte, CommandType>(ref start) = default(TCommand).Type;
            return ref Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType)));
        }

        private ref TCommand Allocate<TCommand, TVariable>() where TCommand : unmanaged, ICommand where TVariable : unmanaged
        {
            var advanceSize = Align(sizeof(CommandType) + sizeof(TCommand) + sizeof(TVariable));
            var data = _buffer.GetSpan(advanceSize);

            ref var start = ref MemoryMarshal.GetReference(data);
            Unsafe.As<byte, CommandType>(ref start) = default(TCommand).Type;
            return ref Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType)));
        }

        private ref TCommand Allocate<TCommand, TVariable>(int count, out int advanceSize) where TCommand : unmanaged, ICommand where TVariable : unmanaged
        {
            advanceSize = Align(sizeof(CommandType) + sizeof(TCommand) + sizeof(TVariable) * (int)count);
            var data = _buffer.GetSpan(advanceSize);

            ref var start = ref MemoryMarshal.GetReference(data);
            Unsafe.As<byte, CommandType>(ref start) = default(TCommand).Type;
            return ref Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType)));
        }

        public CommandBuffer Buffer => new CommandBuffer { Buffer = _buffer.WrittenMemory, FirstPipeline = _first };
    }
}
