using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Games.RohBot
{
    public class WebSocket : IDisposable
    {
        public delegate void ConnectedEvent(bool success);
        public delegate void DisconnectedEvent(Exception exception);
        public delegate void MessageReceivedEvent(string message);

        private TcpClient _client;
        private Random _random;

        public ConnectedEvent Connected;
        public DisconnectedEvent Disconnected;
        public MessageReceivedEvent MessageReceived;

        public WebSocket(string host, int port)
        {
            _client = new TcpClient();
            _random = new Random();
            
            Connect(host, port);
        }

        public void Dispose()
        {
            if (_client == null)
                return;

            _client.Close();
            _client = null;
        }

        public void Send(string message)
        {
            if (_client == null || !_client.Connected)
                throw new Exception("connection closed");

            var payload = Encoding.UTF8.GetBytes(message);

            var key = new byte[4];
            _random.NextBytes(key);

            MaskData(payload, key);

            var payloadLen = payload.Length;
            if (payloadLen > ushort.MaxValue)
                payloadLen = 127;
            else if (payloadLen >= 126)
                payloadLen = 126;

            var stream = _client.GetStream();

            var header = new byte[2];
            header[0] = 0x81; // fin, text
            header[1] = (byte)(0x80 | ((byte)payloadLen & 0x7F)); // mask, payload len

            stream.Write(header, 0, header.Length);

            if (payloadLen == 126)
            {
                var extPayloadLen = new byte[2];
                extPayloadLen[0] = (byte)((payload.Length >> 8) & 0xFF);
                extPayloadLen[1] = (byte)(payload.Length & 0xFF);

                stream.Write(extPayloadLen, 0, extPayloadLen.Length);
            }

            if (payloadLen == 127)
            {
                var extPayloadLen = new byte[8];
                extPayloadLen[3] = (byte)((payload.Length >> 24) & 0xFF);
                extPayloadLen[2] = (byte)((payload.Length >> 16) & 0xFF);
                extPayloadLen[1] = (byte)((payload.Length >> 8) & 0xFF);
                extPayloadLen[0] = (byte)(payload.Length & 0xFF);

                stream.Write(extPayloadLen, 0, extPayloadLen.Length);
            }

            stream.Write(key, 0, key.Length);

            stream.Write(payload, 0, payload.Length);
        }

        private static void MaskData(byte[] data, byte[] key)
        {
            if (key.Length != 4)
                throw new ArgumentException("key must be 4 bytes");

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ key[i % 4]);
            }
        }

        private void ReadFramePayload(bool fin, long length, List<byte> data = null)
        {
            if (length == 0)
            {
                // read next frame
                ReadFrame();
                return;
            }

            data = data ?? new List<byte>();

            var stream = _client.GetStream();
            var buffer = new byte[length];

            ReadFull(stream, buffer, 0, buffer.Length, success =>
            {
                if (!success)
                    throw new Exception();

                data.AddRange(buffer);

                if (fin)
                {
                    var dataText = Encoding.UTF8.GetString(data.ToArray());

                    Dispatcher.Enqueue(() =>
                    {
                        if (MessageReceived != null)
                            MessageReceived(dataText);
                    });

                    // read next frame
                    ReadFrame();
                    return;
                }

                // need more data
                ReadFrame(data);
            });
        }   

        private void ReadFrameHeader64(bool fin, List<byte> data = null)
        {
            var stream = _client.GetStream();
            var buffer = new byte[8];

            ReadFull(stream, buffer, 0, buffer.Length, success =>
            {
                if (!success)
                    throw new Exception();

                var lengthUpper = (buffer[0] << 56) | (buffer[1] << 48) | (buffer[2] << 40) | (buffer[3] << 32);
                var lengthLower = (buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7];

                var length = ((long)lengthUpper << 32) | (uint)lengthLower;
                ReadFramePayload(fin, length, data);
            });
        }

        private void ReadFrameHeader16(bool fin, List<byte> data = null)
        {
            var stream = _client.GetStream();
            var buffer = new byte[2];

            ReadFull(stream, buffer, 0, buffer.Length, success =>
            {
                if (!success)
                    throw new Exception();

                var length = (buffer[0] << 8) | buffer[1];
                ReadFramePayload(fin, length, data);
            });
        }

        private void ReadFrame(List<byte> data = null)
        {
            var stream = _client.GetStream();
            var buffer = new byte[2];

            ReadFull(stream, buffer, 0, buffer.Length, success =>
            {
                if (!success)
                    throw new Exception();

                var header = (buffer[0] << 8) | buffer[1];

                var fin = (header & 0x8000) != 0;
                var opcode = (header >> 8) & 0x0F;
                var mask = (header & 0x80) != 0;

                // connection close
                if (opcode == 0x8)
                {
                    _client.Close();

                    Dispatcher.Enqueue(() =>
                    {
                        if (Disconnected != null)
                            Disconnected(null);
                    });

                    return;
                }

                if (opcode == 0x2)
                    throw new NotSupportedException("binary frames arent supported");

                if (mask)
                    throw new Exception("why is mask set");
                    
                var length = header & 0x7F;

                if (length == 126)
                {
                    ReadFrameHeader16(fin, data);
                    return;
                }

                if (length == 127)
                {
                    ReadFrameHeader64(fin, data);
                    return;
                }

                ReadFramePayload(fin, length, data);
            });
        }

        private void ReceiveHandshake(StringBuilder line = null, char prevChar = '\0')
        {
            var stream = _client.GetStream();
            var buffer = new byte[1];

            line = line ?? new StringBuilder();

            ReadFull(stream, buffer, 0, 1, success =>
            {
                if (!success)
                {
                    Dispatcher.Enqueue(() => 
                    {
                        if (Connected != null)
                            Connected(false);
                    });

                    return;
                }
                
                var ch = (char)buffer[0];

                // completed line
                if (ch == '\n' && prevChar == '\r')
                {
                    // empty line, got the whole response
                    if (line.Length == 0)
                    {
                        Dispatcher.Enqueue(() =>
                        {
                            if (Connected != null)
                                Connected(true);
                        });
                        
                        ReadFrame();
                        return;
                    }

                    ReceiveHandshake();
                    return;
                }

                if (ch != '\r')
                    line.Append(ch);

                ReceiveHandshake(line, ch);
            });
        }

        private void ReadFull(
            NetworkStream stream, byte[] buffer, int offset, int size, Action<bool> callback)
        {
            stream.BeginRead(buffer, offset, size, Wrap(result =>
            {
                var bytesRead = stream.EndRead(result);

                // read failed
                if (bytesRead == 0)
                {
                    callback(false);
                    return;
                }

                offset += bytesRead;
                size -= bytesRead;

                // done
                if (size == 0)
                {
                    callback(true);
                    return;
                }

                ReadFull(stream, buffer, offset, size, callback);
            }), stream);
        }

        private void SendHandshake(string host)
        {
            var openHandshakeLines = new[]
            {
                "GET / HTTP/1.1",
                "Host: " + host,
                "Upgrade: websocket",
                "Connection: Upgrade",
                "Sec-WebSocket-Key: L6E0FkvSG6QsJ1O4lCtXdQ==",
                "Origin: http://arcade.facepunch.com",
                "Sec-WebSocket-Version: 13"
            };

            var openHandshakeStr = string.Join("\r\n", openHandshakeLines) + "\r\n\r\n";

            var openHandshake = Encoding.ASCII.GetBytes(openHandshakeStr);

            var stream = _client.GetStream();

            stream.BeginWrite(openHandshake, 0, openHandshake.Length, Wrap(result =>
            {
                stream.EndWrite(result);

                ReceiveHandshake();
            }), stream);
        }

        private void Connect(string host, int port)
        {
            _client.BeginConnect(host, port, Wrap(result =>
            {
                if (!_client.Connected)
                {
                    Dispatcher.Enqueue(() =>
                    {
                        if (Connected != null)
                            Connected(false);
                    });
                    
                    return;
                }
                
                _client.EndConnect(result);

                SendHandshake(host);
            }), _client);
        }

        private AsyncCallback Wrap(AsyncCallback callback)
        {
            return result =>
            {
                try
                {
                    callback(result);
                }
                catch (Exception e)
                {
                    Dispatcher.Enqueue(() =>
                    {
                        if (Disconnected != null)
                            Disconnected(e);
                    });
                }
            };
        }
    }
}
