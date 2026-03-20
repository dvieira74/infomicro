using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infomicro.Core
{
    public class NetworkHost
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public event Action<string> CodeGenerated;
        public event Action ClientConnected;
        public event Action ClientDisconnected;
        public event Action<string> CommandReceived;

        public bool IsRunning { get; private set; }
        public bool IsClientConnected { get; private set; }
        public string ServerAddress { get; set; } = "autorack.proxy.rlwy.net";
        public int ServerPort { get; set; } = 32080;

        public async Task StartAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            _cts = new CancellationTokenSource();
            _client = new TcpClient();

            try
            {
                await _client.ConnectAsync(ServerAddress, ServerPort);
                _stream = _client.GetStream();

                byte[] initCmd = Encoding.UTF8.GetBytes("HOST\n");
                await _stream.WriteAsync(initCmd, 0, initCmd.Length);

                // Wait for CODE|XXXXXX
                string codeResponse = await ReadLineAsync(_stream, _cts.Token);
                if (codeResponse != null && codeResponse.StartsWith("CODE|"))
                {
                    string code = codeResponse.Split('|')[1];
                    CodeGenerated?.Invoke(code);

                    // Wait for START
                    string startCmd = await ReadLineAsync(_stream, _cts.Token);
                    if (startCmd == "START")
                    {
                        IsClientConnected = true;
                        ClientConnected?.Invoke();
                        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                    }
                    else
                    {
                        Stop();
                    }
                }
                else
                {
                    Stop();
                }
            }
            catch
            {
                Stop();
            }
        }

        private async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] buf = new byte[256];
            int read = await stream.ReadAsync(buf, 0, buf.Length, token);
            if (read == 0) return null;
            return Encoding.UTF8.GetString(buf, 0, read).TrimEnd('\r', '\n', '\0', ' ');
        }

        public void Stop()
        {
            IsRunning = false;
            IsClientConnected = false;
            _cts?.Cancel();
            DisconnectClient();
        }

        private void DisconnectClient()
        {
            if (_client != null)
            {
                _stream?.Close();
                _client?.Close();
                _client = null;
                _stream = null;
                ClientDisconnected?.Invoke();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsClientConnected)
                {
                    var (type, payload) = await NetworkProtocol.ReceiveMessageAsync(_stream, token);
                    
                    if (payload == null)
                    {
                        // Disconnected
                        break;
                    }

                    if (type == MessageType.Command)
                    {
                        string command = Encoding.UTF8.GetString(payload);
                        CommandReceived?.Invoke(command);
                    }
                }
            }
            catch
            {
                // Connection error
            }
            finally
            {
                Stop();
            }
        }

        public async Task SendImageAsync(byte[] imageData)
        {
            if (!IsClientConnected || _stream == null) return;

            try
            {
                await NetworkProtocol.SendMessageAsync(_stream, MessageType.Image, imageData);
            }
            catch
            {
                Stop();
            }
        }
    }
}
