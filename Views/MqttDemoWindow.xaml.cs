using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace IPWorkbench.Views
{
    public partial class MqttDemoWindow : Window
    {
        private IMqttClient? _client;

        public MqttDemoWindow()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                ClientIdText.Text = "ipworkbench_" + Guid.NewGuid().ToString("N")[..12];
                PublishPayloadText.Text = """{"msg":"hello","from":"IPWorkbench"}""";
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
            if (!int.TryParse(BrokerPortText.Text.Trim(), out var port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("端口应为 1–65535。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var host = BrokerHostText.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("请填写 Broker 地址。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var clientId = string.IsNullOrWhiteSpace(ClientIdText.Text)
                ? "ipworkbench_" + Guid.NewGuid().ToString("N")[..12]
                : ClientIdText.Text.Trim();

            SetUiBusy(true);
            try
            {
                await DisconnectInternalAsync();

                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();
                _client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(host, port)
                    .WithClientId(clientId)
                    .WithCleanSession()
                    .Build();

                await _client.ConnectAsync(options, CancellationToken.None);

                StatusText.Text = $"已连接 {host}:{port}，ClientId={clientId}";
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                SubscribeButton.IsEnabled = true;
                PublishButton.IsEnabled = true;
                AppendReceiveLine("系统：连接成功。");
            }
            catch (Exception ex)
            {
                AppendReceiveLine("系统：连接失败 — " + ex.Message);
                MessageBox.Show(ex.Message, "MQTT 连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
                await DisconnectInternalAsync();
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            SetUiBusy(true);
            try
            {
                await DisconnectInternalAsync();
                StatusText.Text = "未连接";
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                SubscribeButton.IsEnabled = false;
                PublishButton.IsEnabled = false;
                AppendReceiveLine("系统：已断开。");
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        private async Task DisconnectInternalAsync()
        {
            if (_client == null)
                return;

            try
            {
                if (_client.IsConnected)
                    await _client.DisconnectAsync();
            }
            catch
            {
                // ignore
            }

            try
            {
                _client.ApplicationMessageReceivedAsync -= OnApplicationMessageReceivedAsync;
                _client.Dispose();
            }
            catch
            {
                // ignore
            }

            _client = null;
        }

        private Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic ?? "";
            var payload = e.ApplicationMessage.PayloadSegment.Count > 0
                ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                : "";

            Dispatcher.Invoke(() =>
            {
                AppendReceiveLine($"[{DateTime.Now:HH:mm:ss}] {topic}  {payload}");
            });

            return Task.CompletedTask;
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null || !_client.IsConnected)
                return;

            var topic = SubscribeTopicText.Text.Trim();
            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("请填写订阅主题。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetUiBusy(true);
            try
            {
                var sub = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                    .Build();

                await _client.SubscribeAsync(sub, CancellationToken.None);
                AppendReceiveLine($"系统：已订阅 {topic}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "订阅失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null || !_client.IsConnected)
                return;

            var topic = PublishTopicText.Text.Trim();
            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("请填写发布主题。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var payload = PublishPayloadText.Text ?? "";
            SetUiBusy(true);
            try
            {
                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(payload))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _client.PublishAsync(msg, CancellationToken.None);
                AppendReceiveLine($"系统：已发布 → {topic}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "发布失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        private void AppendReceiveLine(string line)
        {
            if (ReceiveLogText.Text.Length > 120_000)
                ReceiveLogText.Clear();

            ReceiveLogText.AppendText(line + Environment.NewLine);
            ReceiveLogText.ScrollToEnd();
        }

        private void SetUiBusy(bool busy)
        {
            Cursor = busy ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }
    }
}
