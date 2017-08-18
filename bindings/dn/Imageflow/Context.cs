﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using imageflow;
using Imageflow.Native;

namespace Imageflow
{
    public class Context: CriticalFinalizerObject, IDisposable
    {
        private IntPtr _ptr; 
        
        internal IntPtr Pointer
        {
            get
            {
                if (_ptr == IntPtr.Zero) throw new ImageflowDisposedException("Context");
                return _ptr;
            }
        }

        public Context()
        {
            _ptr = NativeMethods.imageflow_context_create();
            if (_ptr == IntPtr.Zero) throw new OutOfMemoryException("Failed to create Imageflow Context");
        }

        public bool HasError => NativeMethods.imageflow_context_has_error(Pointer);
        
        
        public JsonResponse SendMessage(string method, byte[] utf8Json)
        {
            
            var pinned = GCHandle.Alloc(utf8Json, GCHandleType.Pinned);
            var methodPinned = GCHandle.Alloc(Encoding.ASCII.GetBytes(method + char.MinValue), GCHandleType.Pinned);
            try
            {
                AssertReady();
                var ptr = NativeMethods.imageflow_context_send_json(Pointer, methodPinned.AddrOfPinnedObject(), pinned.AddrOfPinnedObject(),
                    new UIntPtr((ulong) utf8Json.LongLength));
                AssertReady();
                return new JsonResponse(this, ptr);
            }
            finally
            {
                pinned.Free();
                methodPinned.Free();
            }
        }
        
        public void AssertReady()
        {
            if (HasError) throw ImageflowException.FromContext(this);
        }

        public bool IsDisposed => _ptr == IntPtr.Zero; 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_ptr == IntPtr.Zero) return;

            if (disposing)
            {
                // Free managed objects
            }

            ImageflowException e = null;
            if (!NativeMethods.imageflow_context_begin_terminate(_ptr))
            {
                e = ImageflowException.FromContext(this);
            }
            NativeMethods.imageflow_context_destroy(_ptr);
            _ptr = IntPtr.Zero;
            if (e != null) throw e;

        }

        ~Context()
        {
            Dispose (false);
        }
    }
}