using PTCenter.Api.OpenApiTransformers;

namespace PTCenter.Api;

public static class ApiExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiExtensions(IConfiguration configuration)
        {
            services
                .AddCorsConfig(configuration)
                .AddOpenApiServices();
        
            return services;
        }
    
        private IServiceCollection AddOpenApiServices()
        {
            var serviceProvider = services.BuildServiceProvider();

            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            return services;
        }

        private IServiceCollection AddCorsConfig(IConfiguration configuration)
        {
            var allowedOrigin = configuration.GetSection("AllowedOrigin").Get<string[]>();
            
            services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", builder =>
                {
                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowAnyOrigin();
                });
            });
            
            return services;
        }
    }
    
}