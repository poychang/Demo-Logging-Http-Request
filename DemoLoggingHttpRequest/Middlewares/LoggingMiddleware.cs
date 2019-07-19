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
            /*
             請注意，HTTP Request Body 是個特殊的 Stream，只能被讀取一次
             且當 HTTP Conetxt 經過 MVC 中介程序後，HTTP Request Body 會因為被讀取過而消失
             如果希望在整個 Pipeline 流程中保留原始的 HTTP Request Body 資料，請在 MVC 中介程序之前，暫存相關資料
             以便後續使用
             */
            var log = $"{context.Request.Path}, {context.Request.Method}, {ReadRequestBody(context)}";
            _logger.LogTrace(log);

            await _next.Invoke(context);
        }

        /// <summary>
        /// 讀取 HTTP Request 的 Body 資料
        /// </summary>
        /// <param name="context">HTTP 的上下文</param>
        /// <returns></returns>
        private string ReadRequestBody(HttpContext context)
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
                var body = bodyReader.ReadToEnd();

                // 將 HTTP Request 的 Stream 起始位置歸零
                context.Request.Body.Position = 0;

                return body;
            }
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
