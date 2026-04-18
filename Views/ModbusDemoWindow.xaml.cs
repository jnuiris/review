using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NModbus;
using NModbus.Device;

namespace IPWorkbench.Views
{
    public partial class ModbusDemoWindow : Window
    {
        private TcpClient? _tcpClient;
        private IModbusMaster? _master;

        public ModbusDemoWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectInternal();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ushort.TryParse(PortText.Text.Trim(), out var port))
            {
                MessageBox.Show("端口必须是 0–65535 的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var host = HostText.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("请填写主机地址。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(host, port).ConfigureAwait(true);
                var factory = new ModbusFactory();
                _master = factory.CreateMaster(client);
                _tcpClient = client;

                StatusText.Text = $"已连接 {host}:{port}";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                ReadButton.IsEnabled = true;
                WriteButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败：{ex.Message}", "Modbus", MessageBoxButton.OK, MessageBoxImage.Error);
                DisconnectInternal();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            DisconnectInternal();
            StatusText.Text = "未连接";
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            ReadButton.IsEnabled = false;
            WriteButton.IsEnabled = false;
        }

        private void DisconnectInternal()
        {
            try
            {
                _master?.Dispose();
            }
            catch
            {
                // ignore
            }

            _master = null;

            try
            {
                _tcpClient?.Close();
            }
            catch
            {
                // ignore
            }

            _tcpClient = null;
        }

        private bool TryGetSlaveId(out byte slaveId)
        {
            slaveId = 1;
            if (!byte.TryParse(SlaveIdText.Text.Trim(), out var s) || s == 0)
            {
                MessageBox.Show("站号必须是 1–255。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            slaveId = s;
            return true;
        }

        private async void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_master == null || !TryGetSlaveId(out var slaveId))
                return;

            if (!ushort.TryParse(ReadStartText.Text.Trim(), out var start))
            {
                MessageBox.Show("起始地址应为 0–65535。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ushort.TryParse(ReadCountText.Text.Trim(), out var count) || count == 0 || count > 125)
            {
                MessageBox.Show("数量应为 1–125。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                ushort[] regs = await Task.Run(() => _master.ReadHoldingRegisters(slaveId, start, count)).ConfigureAwait(true);
                var sb = new StringBuilder();
                for (var i = 0; i < regs.Length; i++)
                {
                    sb.AppendLine($"地址 {start + i} = {regs[i]} (0x{regs[i]:X4})");
                }

                ReadResultText.Text = sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                ReadResultText.Text = "读取失败：" + ex.Message;
                MessageBox.Show(ex.Message, "读取失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_master == null || !TryGetSlaveId(out var slaveId))
                return;

            if (!ushort.TryParse(WriteAddrText.Text.Trim(), out var addr))
            {
                MessageBox.Show("地址应为 0–65535。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ushort.TryParse(WriteValueText.Text.Trim(), out var value))
            {
                MessageBox.Show("值应为 0–65535。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                await Task.Run(() => _master.WriteSingleRegister(slaveId, addr, value)).ConfigureAwait(true);
                MessageBox.Show($"已写入 地址 {addr} = {value}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "写入失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            ConnectButton.IsEnabled = !busy && _master == null;
            DisconnectButton.IsEnabled = !busy && _master != null;
            ReadButton.IsEnabled = !busy && _master != null;
            WriteButton.IsEnabled = !busy && _master != null;
            Cursor = busy ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }
    }
}
