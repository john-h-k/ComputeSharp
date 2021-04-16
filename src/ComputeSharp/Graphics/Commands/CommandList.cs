using ComputeSharp.Resources;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.NativeApi;

namespace ComputeSharp.Graphics.Commands
{
    internal unsafe struct CommandList
    {
        private ArrayBufferWriter<byte> _buffer;
        private PipelineHandle _first;

        public static CommandList Create()
            => new CommandList() { _buffer = new ArrayBufferWriter<byte>(64) };

        public void CopyBuffer(BufferHandle source, BufferHandle dest, uint length)
            => CopyBuffer(source, 0, dest, 0, length);
        public void CopyBuffer(BufferHandle source, uint srcOffset, BufferHandle dest, uint destOffset, uint length)
        {
            var span = _buffer.GetSpan(sizeof(CommandBufferCopy));

            ref var cmd = ref Unsafe.As<byte, CommandBufferCopy>(ref span[0]);

            cmd.Source = source;
            cmd.Dest = dest;
            cmd.SourceOffset = srcOffset;
            cmd.DestOffset = destOffset;
            cmd.Length = length;

            _buffer.Advance(sizeof(CommandBufferCopy));
        }

        public void ResourceTransition(TextureHandle texture, ResourceState before, ResourceState after)
        {
            var size = sizeof(CommandTransitions) + sizeof(ResourceTransitionBarrier);
            var span = _buffer.GetSpan(size);

            ref var cmd = ref Unsafe.As<byte, CommandTransitions>(ref span[0]);
            ref var barrier = ref Unsafe.As<byte, ResourceTransitionBarrier>(ref Unsafe.Add(ref span[0], sizeof(CommandTransitions)));

            cmd.Count = 1;
            barrier.Resource = texture;
            barrier.Subresource = uint.MaxValue;
            barrier.Before = before;
            barrier.After = after;

            _buffer.Advance(size);
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
            var span = _buffer.GetSpan(sizeof(CommandBufferToTextureCopy) + sizeof(Box));

            ref var cmd = ref Unsafe.As<byte, CommandBufferToTextureCopy>(ref span[0]);
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

            _buffer.Advance(sizeof(CommandBufferToTextureCopy) + sizeof(Box));
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
            var span = _buffer.GetSpan(sizeof(CommandBufferToTextureCopy));

            ref var cmd = ref Unsafe.As<byte, CommandBufferToTextureCopy>(ref span[0]);

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

            _buffer.Advance(sizeof(CommandBufferToTextureCopy));
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
            var span = _buffer.GetSpan(sizeof(CommandTextureToBufferCopy) + sizeof(Box));

            ref var cmd = ref Unsafe.As<byte, CommandTextureToBufferCopy>(ref span[0]);
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

            _buffer.Advance(sizeof(CommandTextureToBufferCopy) + sizeof(Box));
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
            var span = _buffer.GetSpan(sizeof(CommandTextureToBufferCopy) + sizeof(Box));

            ref var cmd = ref Unsafe.As<byte, CommandTextureToBufferCopy>(ref span[0]);
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

            _buffer.Advance(sizeof(CommandTextureToBufferCopy));
        }

        public void SetPipeline(PipelineHandle handle)
        {
            if (_first == default)
            {
                _first = handle;
            }

            var span = _buffer.GetSpan(sizeof(CommandSetPipeline));

            ref var cmd = ref Unsafe.As<byte, CommandSetPipeline>(ref span[0]);

            cmd.Pipeline = handle;

            _buffer.Advance(sizeof(CommandSetPipeline));
        }

        public void SetConstants(uint paramIndex, ReadOnlySpan<uint> data)
        {
            var size = sizeof(CommandBind32BitConstants) + sizeof(uint) * data.Length;
            var span = _buffer.GetSpan(size);

            ref var cmd = ref Unsafe.As<byte, CommandBind32BitConstants>(ref span[0]);
            ref var constants = ref Unsafe.As<byte, uint>(ref Unsafe.Add(ref span[0], sizeof(CommandBind32BitConstants)));

            cmd.BindPoint = BindPoint.Compute;
            cmd.ParameterIndex = paramIndex;
            cmd.Num32BitValues = (uint)data.Length;

            data.CopyTo(MemoryMarshal.CreateSpan(ref constants, data.Length));

            _buffer.Advance(size);
        }

        public void SetResources(uint paramIndex, ReadOnlySpan<DescriptorSetHandle> resources)
        {
            var size = sizeof(CommandBindDescriptors) + sizeof(DescriptorSetHandle) * resources.Length;
            var span = _buffer.GetSpan(size);

            ref var cmd = ref Unsafe.As<byte, CommandBindDescriptors>(ref span[0]);
            ref var descriptors = ref Unsafe.As<byte, DescriptorSetHandle>(ref Unsafe.Add(ref span[0], sizeof(CommandBindDescriptors)));

            cmd.BindPoint = BindPoint.Compute;
            cmd.FirstSetIndex = paramIndex;
            cmd.SetCount = (uint)resources.Length;

            resources.CopyTo(MemoryMarshal.CreateSpan(ref descriptors, resources.Length));

            _buffer.Advance(size);
        }

        public void Dispatch(uint x, uint y, uint z)
        {
            var span = _buffer.GetSpan(sizeof(CommandDispatch));

            ref var cmd = ref Unsafe.As<byte, CommandDispatch>(ref span[0]);

            cmd.X = x;
            cmd.Y = y;
            cmd.Z = z;

            _buffer.Advance(sizeof(CommandDispatch));
        }

        public CommandBuffer Buffer => new CommandBuffer { Buffer = _buffer.WrittenMemory, FirstPipeline = _first };
    }
}
