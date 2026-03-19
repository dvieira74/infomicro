using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Infomicro.Services
{
    public class InputSimulator
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        public void ExecuteMouseCommand(string command)
        {
            try
            {
                var parts = command.Split('|');
                if (parts.Length < 4 || parts[0] != "MOUSE") return;

                string action = parts[1];
                double xRatio = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                double yRatio = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);

                int absX = (int)(xRatio * 65535);
                int absY = (int)(yRatio * 65535);

                int flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
                
                if (action == "LEFT_DOWN") flags |= MOUSEEVENTF_LEFTDOWN;
                else if (action == "LEFT_UP") flags |= MOUSEEVENTF_LEFTUP;
                else if (action == "RIGHT_DOWN") flags |= MOUSEEVENTF_RIGHTDOWN;
                else if (action == "RIGHT_UP") flags |= MOUSEEVENTF_RIGHTUP;

                mouse_event(flags, absX, absY, 0, 0);
            }
            catch
            {
                // Ignore parsing or execution errors
            }
        }

        private const int KEYEVENTF_KEYUP = 0x0002;

        public void ExecuteKeyboardCommand(string command)
        {
            try
            {
                var parts = command.Split('|');
                if (parts.Length < 3 || parts[0] != "KEYBOARD") return;

                string action = parts[1];
                if (!byte.TryParse(parts[2], out byte keycode)) return;

                int flags = 0;
                if (action == "UP") flags |= KEYEVENTF_KEYUP;

                // Simulate key press/release
                keybd_event(keycode, 0, flags, 0);
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
