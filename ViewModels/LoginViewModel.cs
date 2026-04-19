using IPWorkbench.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace IPWorkbench.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly ILoginService _loginService;

        private string _username;
        private string _password;
        private bool _isLoading;
        private string _statusMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // 登录命令
        public RelayCommand LoginCommand { get; }

        /// <summary>
        /// 登录成功后的导航，由 LoginWindow 注入，避免 ViewModel 直接引用具体窗口类型。
        /// </summary>
        public Action? OnLoginSucceeded { get; set; }

        public LoginViewModel()
        {
            // DemoBack: POST {基地址}/api/login
            _loginService = new HttpLoginService(LoginApiSettings.BaseUrl);

            LoginCommand = new RelayCommand(
                execute: async _ => await ExecuteLogin(),
                canExecute: _ => !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)
            );
        }

        private async Task ExecuteLogin()
        {
            IsLoading = true;
            StatusMessage = "正在登录...";

            try
            {
                var result = await _loginService.LoginAsync(Username, Password);

                if (result.IsSuccess)
                {
                    StatusMessage = $"登录成功! Token: {result.Token}";
                    // TODO: 在这里保存 Token 到本地存储或全局状态
                    if (OnLoginSucceeded != null)
                    {
                        Application.Current.Dispatcher.Invoke(OnLoginSucceeded);
                    }
                    else
                    {
                        MessageBox.Show($"登录成功！\nToken: {result.Token}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    StatusMessage = result.Message;
                    MessageBox.Show(result.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "发生异常: " + ex.Message;
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                // 通知命令管理器重新评估 CanExecute，从而更新按钮状态
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
