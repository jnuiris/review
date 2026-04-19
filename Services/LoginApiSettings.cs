namespace IPWorkbench.Services
{
    /// <summary>
    /// DemoBack 基地址，默认与本地 <c>launchSettings.json</c> 中 http 端口一致。
    /// 可在应用启动时修改（例如将来从配置文件读取）。
    /// </summary>
    public static class LoginApiSettings
    {
        public static string BaseUrl { get; set; } = "http://localhost:5198";
    }
}
