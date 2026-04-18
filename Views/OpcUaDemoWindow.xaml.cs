using System;
using System.Threading.Tasks;
using System.Windows;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace IPWorkbench.Views
{
    public partial class OpcUaDemoWindow : Window
    {
        private OpcClient? _client;

        public OpcUaDemoWindow()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                NodeIdText.Text = "ns=0;i=2258";
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            await DisconnectInternalAsync();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Uri.TryCreate(EndpointText.Text.Trim(), UriKind.Absolute, out var uri))
            {
                MessageBox.Show("请输入有效的服务端点 URI，例如 opc.tcp://127.0.0.1:4840", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                await DisconnectInternalAsync();

                _client = await Task.Run(() =>
                {
                    var client = new OpcClient(uri, new OpcSecurityPolicy(OpcSecurityMode.None));
                    client.Connect();
                    return client;
                });

                StatusText.Text = $"已连接 {uri}";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                ReadButton.IsEnabled = true;
                ReadResultText.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OPC UA 连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
                await DisconnectInternalAsync();
                StatusText.Text = "未连接";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            SetBusy(true);
            try
            {
                await DisconnectInternalAsync();
                StatusText.Text = "未连接";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                ReadButton.IsEnabled = false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private Task DisconnectInternalAsync()
        {
            var c = _client;
            _client = null;
            if (c == null)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                try
                {
                    c.Disconnect();
                }
                catch
                {
                    // ignore
                }

                try
                {
                    c.Dispose();
                }
                catch
                {
                    // ignore
                }
            });
        }

        private async void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null)
                return;

            var nodeId = NodeIdText.Text.Trim();
            if (string.IsNullOrEmpty(nodeId))
            {
                MessageBox.Show("请填写 NodeId。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                var text = await Task.Run(() =>
                {
                    var value = _client.ReadNode(nodeId);
                    return value.ToString();
                });

                ReadResultText.Text = text;
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

        private void SetBusy(bool busy)
        {
            Cursor = busy ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }
    }
}
