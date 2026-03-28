using Microsoft.OpenApi.Models;

namespace DocToSpeechAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddScoped<AzureAiService>();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Exam API",
                    Version = "v1"
                });
            });

            var app = builder.Build();

            // IMPORTANT: enable swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exam API v1");
                c.RoutePrefix = "swagger"; // default
            });

            app.UseHttpsRedirection();

            app.MapControllers();

            app.Run();
        }
    }
}