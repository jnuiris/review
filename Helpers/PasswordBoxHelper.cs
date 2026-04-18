// Helpers/PasswordBoxHelper.cs
using System.Windows;
using System.Windows.Controls;

namespace IPWorkbench.Helpers
{
    public static class PasswordBoxHelper
    {
        // 定义附加属性
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPasswordProperty);
        }

        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPasswordProperty, value);
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                // 移除旧的事件监听，防止重复触发
                passwordBox.PasswordChanged -= HandlePasswordChanged;

                // 如果新值与当前密码不同，则更新密码框内容
                if (passwordBox.Password != (string)e.NewValue)
                {
                    passwordBox.Password = (string)e.NewValue;
                }

                // 添加新的事件监听
                passwordBox.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                // 当用户输入时，将密码框的值同步回附加属性，进而同步到 ViewModel
                SetBoundPassword(passwordBox, passwordBox.Password);
            }
        }
    }
}