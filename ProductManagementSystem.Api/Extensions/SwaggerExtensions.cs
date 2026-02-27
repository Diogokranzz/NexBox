using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProductManagementSystem.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Product Management System API",
                Version = "v1.0.0",
                Description = "API completa para gerenciamento de produtos com autenticação JWT",
                Contact = new OpenApiContact
                {
                    Name = "Diogo Kranz",
                    Email = "diogo@example.com",
                    Url = new Uri("https://github.com/diogo")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header usando Bearer scheme"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<SwaggerOperationFilter>();
        });

        return services;
    }

    public static WebApplication UseCustomSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsProduction()) // Keeping for test purpose in dev/prod
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductManagementSystem API v1.0");
                options.RoutePrefix = string.Empty; 
                options.DocumentTitle = "Product Management System - API Documentation";
                options.DefaultModelsExpandDepth(0); 
            });
        }

        return app;
    }
}

public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Tags.Count == 0) return;
        operation.Summary ??= context.ApiDescription.HttpMethod;
    }
}
