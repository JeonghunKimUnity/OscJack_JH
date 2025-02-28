// OSC Jack - Open Sound Control plugin for Unity
// https://github.com/keijiro/OscJack

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OscJack
{
    public sealed class OscServer : IDisposable
    {
        #region Public Properties And Methods

        public OscMessageDispatcher MessageDispatcher {
            get { return _dispatcher; }
        }

        public OscServer(int listenPort)
        {
            _dispatcher = new OscMessageDispatcher();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // On some platforms, it's not possible to cancel Receive() by
            // just calling Close() -- it'll block the thread forever!
            // Therefore, we heve to set timeout to break ServerLoop.
            _socket.ReceiveTimeout = 100;

            _socket.Bind(new IPEndPoint(IPAddress.Any, listenPort));

            _thread = new Thread(ServerLoop);
            _thread.Start();

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        }

        #endregion

        #region IDispose implementation

        void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }

                if (_thread != null)
                {
                    _thread.Join();
                    _thread = null;
                }

                _dispatcher = null;
            }
        }

        ~OscServer()
        {
            Dispose(false);
        }

        #endregion

        #region For editor functionalities

        #endregion

        #region Private Objects And Methods

        OscMessageDispatcher _dispatcher;

        Socket _socket;
        Thread _thread;
        bool _disposed;

        void ServerLoop()
        {
            var parser = new OscPacketParser(_dispatcher);
            var buffer = new byte[4096];

            while (!_disposed)
            {
                try
                {
                    int dataRead = _socket.Receive(buffer);
                    if (!_disposed && dataRead > 0)
                        parser.Parse(buffer, dataRead);
                }
                catch (SocketException)
                {
                    // It might exited by timeout. Nothing to do.
                }
                catch (ThreadAbortException)
                {
                    // Abort silently.
                }
                catch (Exception e)
                {
                #if UNITY_EDITOR || UNITY_STANDALONE
                    if (!_disposed) UnityEngine.Debug.Log(e);
                #else
                    if (!_disposed) System.Console.WriteLine(e);
                #endif
                    break;
                }
            }
        }

        #endregion
    }
}
