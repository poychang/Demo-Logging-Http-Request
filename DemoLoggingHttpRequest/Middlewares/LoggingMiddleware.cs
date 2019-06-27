using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DemoLoggingHttpRequest.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 任務調用
        /// </summary>
        /// <param name="context">HTTP 的上下文</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            // 確保 HTTP Request 可以多次讀取
            context.Request.EnableBuffering();

            // 讀取 HTTP Request Body 內容
            // 注意！要設定 leaveOpen 屬性為 true 使 StreamReader 關閉時，HTTP Request 的 Stream 不會跟著關閉
            using (var bodyReader = new StreamReader(stream: context.Request.Body,
                                                     encoding: Encoding.UTF8,
                                                     detectEncodingFromByteOrderMarks: false,
                                                     bufferSize: 1024,
                                                     leaveOpen: true))
            {
                var body = await bodyReader.ReadToEndAsync();
                var log = $"{context.Request.Path}, {context.Request.Method}, {body}";
                _logger.LogTrace(log);
            }

            // 將 HTTP Request 的 Stream 起始位置歸零
            context.Request.Body.Position = 0;

            await _next.Invoke(context);
        }
    }

    public static class LoggingMiddlewareExtensions
    {
        /// <summary>在中介程序中收集 HTTP Request 資訊</summary>
        /// <param name="builder">中介程序建構器</param>
        /// <returns></returns>
        public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware>();
        }
    }
}
