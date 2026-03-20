using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infomicro.Core
{
    public class NetworkClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<byte[]> ImageReceived;

        public bool IsConnected => _client != null && _client.Connected;
        public string ServerAddress { get; set; } = "autorack.proxy.rlwy.net";
        public int ServerPort { get; set; } = 32080;

        public async Task ConnectAsync(string code)
        {
            if (IsConnected) return;

            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(ServerAddress, ServerPort);
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();

                byte[] initCmd = Encoding.UTF8.GetBytes($"CONNECT|{code}\n");
                await _stream.WriteAsync(initCmd, 0, initCmd.Length);

                string response = await ReadLineAsync(_stream, _cts.Token);
                if (response == "OK")
                {
                    Connected?.Invoke();
                    _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                }
                else
                {
                    throw new Exception("Código não encontrado no servidor.");
                }
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        private async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] buf = new byte[256];
            int read = await stream.ReadAsync(buf, 0, buf.Length, token);
            if (read == 0) return null;
            return Encoding.UTF8.GetString(buf, 0, read).TrimEnd('\r', '\n', '\0', ' ');
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _client = null;
            _stream = null;
            Disconnected?.Invoke();
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    var (type, payload) = await NetworkProtocol.ReceiveMessageAsync(_stream, token);

                    if (payload == null)
                    {
                        // Disconnected
                        break;
                    }

                    if (type == MessageType.Image)
                    {
                        ImageReceived?.Invoke(payload);
                    }
                }
            }
            catch
            {
                // Connection error
            }
            finally
            {
                Disconnect();
            }
        }

        public async Task SendCommandAsync(string command)
        {
            if (!IsConnected || _stream == null) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command);
                await NetworkProtocol.SendMessageAsync(_stream, MessageType.Command, data);
            }
            catch
            {
                Disconnect();
            }
        }
    }
}
