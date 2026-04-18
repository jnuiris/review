using System.Windows;

namespace IPWorkbench.Views
{
    public partial class ProtocolDemoWindow : Window
    {
        public ProtocolDemoWindow()
        {
            InitializeComponent();
        }

        private void ModbusDemo_Click(object sender, RoutedEventArgs e)
        {
            var win = new ModbusDemoWindow
            {
                Owner = this
            };
            win.Show();
        }

        private void MqttDemo_Click(object sender, RoutedEventArgs e)
        {
            var win = new MqttDemoWindow { Owner = this };
            win.Show();
        }

        private void OpcUaDemo_Click(object sender, RoutedEventArgs e)
        {
            var win = new OpcUaDemoWindow { Owner = this };
            win.Show();
        }
    }
}
