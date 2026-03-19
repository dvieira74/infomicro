using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Infomicro.Core;

namespace Infomicro.Views
{
    public partial class RemoteViewerWindow : Window
    {
        private readonly NetworkClient _client;

        public RemoteViewerWindow(NetworkClient client)
        {
            InitializeComponent();
            _client = client;
            _client.ImageReceived += Client_ImageReceived;
            _client.Disconnected += Client_Disconnected;
        }

        private void Client_Disconnected()
        {
            Dispatcher.Invoke(() => {
                System.Windows.MessageBox.Show("A conexão com o parceiro foi encerrada.");
                Close();
            });
        }

        private void Client_ImageReceived(byte[] imageData)
        {
            Dispatcher.Invoke(() => {
                try
                {
                    using (var ms = new MemoryStream(imageData))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        imgScreen.Source = bitmap;
                    }
                }
                catch
                {
                    // Ignore bad frames
                }
            });
        }

        private void ImgScreen_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SendMouseCommand("MOVE", e);
        }

        private void ImgScreen_MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string action = e.ButtonState == MouseButtonState.Pressed ? "DOWN" : "UP";
            string btn = e.ChangedButton == MouseButton.Left ? "LEFT" : "RIGHT";
            
            SendMouseCommand($"{btn}_{action}", e);
        }

        private void SendMouseCommand(string action, System.Windows.Input.MouseEventArgs e)
        {
            if (imgScreen.Source == null) return;

            var pos = e.GetPosition(imgScreen);
            
            // Calculate proportional position
            double xRatio = pos.X / imgScreen.ActualWidth;
            double yRatio = pos.Y / imgScreen.ActualHeight;

            // Command format: MOUSE|ACTION|X_RATIO|Y_RATIO
            // Example: MOUSE|MOVE|0.5|0.5 or MOUSE|LEFT_DOWN|0.5|0.5
            string cmd = $"MOUSE|{action}|{xRatio:F4}|{yRatio:F4}";
            _ = _client.SendCommandAsync(cmd);
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(e.Key);
            _ = _client.SendCommandAsync($"KEYBOARD|DOWN|{virtualKey}");
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(e.Key);
            _ = _client.SendCommandAsync($"KEYBOARD|UP|{virtualKey}");
        }
    }
}
