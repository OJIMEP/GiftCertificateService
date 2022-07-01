using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GiftCertificateService.Filters
{
    public class SwaggerSecurityScheme : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            bool IsAllowAnonymousMethod = context.MethodInfo.GetCustomAttributes(true).Any(x => x is AllowAnonymousAttribute);

            bool IsAllowAnonymousController = context.MethodInfo.DeclaringType.GetCustomAttributes(true).Any(x => x is AllowAnonymousAttribute);

            bool IsAuthorizeMethod = context.MethodInfo.GetCustomAttributes(true).Any(x => x is AuthorizeAttribute);

            bool IsAuthorizeController = context.MethodInfo.DeclaringType.GetCustomAttributes(true).Any(x => x is AuthorizeAttribute);

            if (IsAuthorizeMethod || (IsAuthorizeController && !IsAllowAnonymousMethod))
            {
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = JwtBearerDefaults.AuthenticationScheme,
                                    Type = ReferenceType.SecurityScheme
                                }
                            }, new string[] { }
                        }
                    }
                };
            }
        }
    }
}
