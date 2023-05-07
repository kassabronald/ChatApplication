using ChatApplication.Exceptions.StorageExceptions;
using Newtonsoft.Json;

namespace ChatApplication.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment webHostEnvironment)
    {
        _next = next;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            int statusCode = 500;
            if (e is StorageUnavailableException)
            {
                statusCode = 503;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                Message = e.Message,
                Exception = SerializeException(e)
            };
                
            var body = JsonConvert.SerializeObject(response);
            await context.Response.WriteAsync(body);
        }
    }

    private string? SerializeException(Exception e)
    {
        if (_webHostEnvironment.IsProduction())
        {
            return null;
        }
        return e.ToString();
    }
}