using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using Infomicro.Core;
using Infomicro.Services;
using Infomicro.Views;

namespace Infomicro
{
    public partial class MainWindow : Window
    {
        private NetworkHost _host;
        private ScreenCaptureService _captureService;
        private bool _isCapturing;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _captureService = new ScreenCaptureService(40L); // 40% JPEG quality
            _host = new NetworkHost();
            _host.CodeGenerated += Host_CodeGenerated;
            _host.ClientConnected += Host_ClientConnected;
            _host.ClientDisconnected += Host_ClientDisconnected;
            _host.CommandReceived += Host_CommandReceived;
            await _host.StartAsync();
        }

        private void Host_CodeGenerated(string code)
        {
            Dispatcher.Invoke(() => {
                txtLocalCode.Text = code;
            });
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _host?.Stop();
        }

        private void Host_ClientConnected()
        {
            _isCapturing = true;
            Task.Run(CaptureAndSendLoop);
            
            Dispatcher.Invoke(() => {
                Title = "Infomicro - [Alguém conectado em você!]";
            });
        }

        private void Host_ClientDisconnected()
        {
            _isCapturing = false;
            
            Dispatcher.Invoke(() => {
                Title = "Infomicro - Acesso Remoto";
            });
        }

        private async Task CaptureAndSendLoop()
        {
            while (_isCapturing && _host.IsClientConnected)
            {
                var img = _captureService.CaptureScreenAndCompress();
                if (img != null)
                {
                    await _host.SendImageAsync(img);
                }
                await Task.Delay(50); // ~20 fps max
            }
        }

        private readonly InputSimulator _inputSimulator = new InputSimulator();

        private void Host_CommandReceived(string command)
        {
            Dispatcher.Invoke(() => {
                if (command.StartsWith("MOUSE"))
                    _inputSimulator.ExecuteMouseCommand(command);
                else if (command.StartsWith("KEYBOARD"))
                    _inputSimulator.ExecuteKeyboardCommand(command);
            });
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string code = txtRemoteCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                System.Windows.MessageBox.Show("Digite o Código do parceiro.");
                return;
            }

            btnConnect.IsEnabled = false;
            btnConnect.Content = "Conectando...";

            var client = new NetworkClient();
            try
            {
                await client.ConnectAsync(code);
                // Open viewer window
                var viewer = new RemoteViewerWindow(client);
                viewer.Show();
                this.Hide();

                viewer.Closed += (s, args) => 
                {
                    client.Disconnect();
                    this.Show();
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao conectar: {ex.Message}");
            }
            finally
            {
                btnConnect.IsEnabled = true;
                btnConnect.Content = "Conectar";
            }
        }
    }
}