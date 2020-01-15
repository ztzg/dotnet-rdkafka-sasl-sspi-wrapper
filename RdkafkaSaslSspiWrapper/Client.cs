using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RdkafkaSaslSspiWrapper
{
    public class ClientParams
    {
        public string Mechanisms { get; set; }
        public string Service { get; set; }
        public string Hostname { get; set; }
        public string Realm { get; set; }
        public Action<int, string, string> Log { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetType().Name).Append("{")
                .Append(" Mechanisms: ").Append(Mechanisms)
                .Append(" Service: ").Append(Service)
                .Append(" Hostname: ").Append(Hostname)
                .Append(" Realm: ").Append(Realm)
                .Append(" }");
            return sb.ToString();
        }
    }

    public class Client : IDisposable
    {
        private IntPtr handle;
        private IntPtr inBuf;
        private UIntPtr inBufSize;
        private IntPtr outBuf;
        private UIntPtr outBufSize;
        private UIntPtr initialPacketSize;
        private IntPtr errBuf;
        private UIntPtr errBufSize;
        private Delegate logDelegate;

        internal Client(IntPtr handle,
                        IntPtr inBuf, UIntPtr inBufSize,
                        IntPtr outBuf, UIntPtr outBufSize,
                        UIntPtr initialPacketSize,
                        IntPtr errBuf, UIntPtr errBufSize,
                        Delegate logDelegate)
        {
            this.handle = handle;
            this.inBuf = inBuf;
            this.inBufSize = inBufSize;
            this.outBuf = outBuf;
            this.outBufSize = outBufSize;
            this.initialPacketSize = initialPacketSize;
            this.errBuf = errBuf;
            this.errBufSize = errBufSize;
            this.logDelegate = logDelegate;
        }

        public bool InitialPacket(out byte[] clientout)
        {
            if (initialPacketSize.ToUInt32() > 0)
            {
                clientout = new byte[initialPacketSize.ToUInt32()];
                Marshal.Copy(this.outBuf, clientout, 0, clientout.Length);
                initialPacketSize = UIntPtr.Zero;
                return true;
            }
            else
            {
                clientout = null;
                return false;
            }
        }

        public bool Step(byte[] serverin, out byte[] clientout)
        {
            UIntPtr inSize = serverin == null ?
                UIntPtr.Zero : new UIntPtr((uint)serverin.Length);

            if (inSize.ToUInt32() > this.inBufSize.ToUInt32())
            {
                throw new Exception
                    (string.Format
                     ("SASL token too big: {0} bytes, max. {1} supported",
                      inSize.ToUInt32(), this.inBufSize.ToUInt32()));
            }

            if (inSize.ToUInt32() > 0)
            {
                Marshal.Copy(serverin, 0, this.inBuf, (int)inSize.ToUInt32());
            }

            initialPacketSize = UIntPtr.Zero;
            UIntPtr outSize = this.outBufSize;

            int r = Native.rd_kafka_sasl_step(this.handle,
                                              this.inBuf, inSize,
                                              this.outBuf, ref outSize,
                                              this.errBuf, this.errBufSize);
            Library.CheckReturn(r, "rd_kafka_sasl_step", this.errBuf);

            bool mustContinue = r == 1;

            if (outSize.ToUInt32() > 0)
            {
                clientout = new byte[outSize.ToUInt32()];
                Marshal.Copy(this.outBuf, clientout, 0, clientout.Length);
            }
            else
            {
                clientout = null;
            }

            return mustContinue;
        }

        public void Close()
        {
            if (this.inBuf != IntPtr.Zero)
            {
                Native.rd_kafka_sasl_close(this.handle);

                Marshal.FreeHGlobal(this.inBuf);

                this.inBuf = IntPtr.Zero;
                this.outBuf = IntPtr.Zero;
                this.errBuf = IntPtr.Zero;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                // Unmanaged state.
                if (handle != IntPtr.Zero)
                {
                    Native.rd_kafka_sasl_wrapper_free(handle);
                    handle = IntPtr.Zero;
                }

                logDelegate = null;

                disposedValue = true;
            }
        }

        ~Client()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
