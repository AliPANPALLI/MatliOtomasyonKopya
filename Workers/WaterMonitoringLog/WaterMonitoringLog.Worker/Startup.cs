using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(); // API Controller'ları ekleyin
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "RahmiyeTesisLogWorker", Version = "v1" })); // Use OpenApiInfo from Microsoft.OpenApi.Models
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins", builder =>
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("AllowAllOrigins");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers(); // API rotalarını bağlayın
        });
    }
}