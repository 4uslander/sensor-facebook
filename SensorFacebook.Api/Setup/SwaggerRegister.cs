using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SensorFacebook.Api.Setup
{
    public static class SwaggerRegister
    {
        public static IServiceCollection AddApiSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                // Basic info
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SensorFacebook API",
                    Version = "v1",
                    Description = "API for SensorFacebook"
                });

                // JWT Bearer security
                var jwtScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập: Bearer {token}",
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                };
                options.AddSecurityDefinition("Bearer", jwtScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtScheme, Array.Empty<string>() }
                });

                // Gán XML docs (Api + Application + Infrastructure nếu có)
                var baseDir = AppContext.BaseDirectory;
                foreach (var xml in Directory.EnumerateFiles(baseDir, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    options.IncludeXmlComments(xml, includeControllerXmlComments: true);
                }

            });

            return services;
        }
    }

}
