﻿using System;
using System.Runtime.CompilerServices;
using Voltium.Core.Devices;

namespace ComputeSharp.Interop
{
    /// <summary>
    /// Base class for a <see cref="IDisposable"/> class.
    /// </summary>
    public abstract class NativeObject : IDisposable
    {
        protected INativeDevice device;

        internal INativeDevice NativeDevice => this.device;

        public NativeObject(INativeDevice device)
        {
            this.device = device;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations.
        /// </summary>
        ~NativeObject()
        {
            _ = CheckAndDispose();
        }

        /// <summary>
        /// Gets whether or not the current instance has already been disposed.
        /// </summary>
        internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Releases all the native resources for the current instance.
        /// </summary>
        public void Dispose()
        {
            if (CheckAndDispose())
            {
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        private bool CheckAndDispose()
        {
            if (!IsDisposed)
            {
                return IsDisposed = OnDispose();
            }

            return true;
        }

        /// <summary>
        /// Releases unmanaged and (optionally) managed resources.
        /// </summary>
        /// <returns>
        /// Whether or not the dispose has actually been executed. This is done in order to allow derived types to
        /// optionally cancel a dispose operation, by not releasing resources and returning <see langword="false"/>.
        /// </returns>
        protected abstract bool OnDispose();

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the current instance has been disposed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                // We can't use ThrowHelper here as we only want to invoke ToString if we
                // are about to throw an exception. The JIT will recognize this pattern
                // as this method has a single basic block that always throws an exception.
                void Throw() => throw new ObjectDisposedException(ToString());

                Throw();
            }
        }
    }
}
