using Microsoft.AspNetCore.Builder;
using RahmiyeTesisWorker;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(); // API Controller'ları ekleyin
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "RahmiyeTesisWorker", Version = "v1" }));
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