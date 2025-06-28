using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace TaskManagerApp.Middleware
{
    public class HataYakalaMiddleware
    {
        private readonly RequestDelegate _sonraki;
        private readonly ILogger<HataYakalaMiddleware> _logger;

        public HataYakalaMiddleware(RequestDelegate sonraki, ILogger<HataYakalaMiddleware> logger)
        {
            _sonraki = sonraki;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _sonraki(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen bir hata oluştu.");
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var hataMesaji = new
                {
                    mesaj = "Sunucu tarafında bir hata oluştu.",
                    hata = ex.Message
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(hataMesaji));
            }
        }
    }
}
