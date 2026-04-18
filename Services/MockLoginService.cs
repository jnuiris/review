using IPWorkbench.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPWorkbench.Services
{
    public class MockLoginService: ILoginService
    {
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            // 模拟网络延迟 2秒
            await Task.Delay(2000);

            // 简单模拟验证逻辑
            if (username == "admin" && password == "123456")
            {
                return new LoginResult
                {
                    IsSuccess = true,
                    Token = $"mock_token_{Guid.NewGuid()}",
                    Message = "登录成功"
                };
            }
            else
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Token = null,
                    Message = "账号或密码错误 (提示: admin/123456)"
                };
            }
        }
    }
}
