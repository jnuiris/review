using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IPWorkbench.Models;

namespace IPWorkbench.Services
{
    /// <summary>
    /// 调用 DemoBack <c>POST /api/login</c>，见接口说明文档。
    /// </summary>
    public class HttpLoginService : ILoginService
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 基地址，默认 <c>http://localhost:5198</c>（无尾部斜杠）。
        /// </summary>
        public HttpLoginService(string? baseUrl = null)
        {
            var uri = (baseUrl ?? LoginApiSettings.BaseUrl).TrimEnd('/');
            _http = new HttpClient
            {
                BaseAddress = new Uri(uri + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var body = JsonSerializer.Serialize(new LoginRequestDto { Username = username, Password = password }, JsonOptions);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync("api/login", content).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "无法连接登录服务: " + ex.Message
                };
            }
            catch (TaskCanceledException)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "登录请求超时，请检查服务是否启动。"
                };
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "账号或密码不正确。"
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = $"登录失败 ({(int)response.StatusCode}): " + (string.IsNullOrWhiteSpace(err) ? response.ReasonPhrase : err)
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var dto = await JsonSerializer.DeserializeAsync<LoginResponseDto>(stream, JsonOptions).ConfigureAwait(false);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "登录响应无效：未返回 token。"
                };
            }

            return new LoginResult
            {
                IsSuccess = true,
                Token = dto.Token,
                Message = "登录成功"
            };
        }

        private sealed class LoginRequestDto
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        private sealed class LoginResponseDto
        {
            public string Token { get; set; } = "";
        }
    }
}
