using AuthLibrary.Data;
using GiftCertificateService.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using GiftCertificateService.Installers;

namespace GiftCertificateService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // install all services via IInstaller
            builder.Services.InstallServicesInAssembly(builder.Configuration);

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            builder.Logging.AddProvider(
                new HttpLoggerProvider(
                    builder.Configuration["loggerHost"], 
                    builder.Configuration.GetValue<int>("loggerPortUdp"), 
                    builder.Configuration.GetValue<int>("loggerPortHttp"), 
                    builder.Configuration["loggerEnv"]
                )
            );

            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                           ForwardedHeaders.XForwardedProto
            });

            app.UseCors(corsBuilder => corsBuilder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<List<string>>().ToArray()
                )
            );

            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                
            }
            else
            {
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("./v1/swagger.json", "v1");
                //options.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapRazorPages();
            app.MapControllers();


            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    var db = services.GetRequiredService<DateTimeServiceContext>();
                    //db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }

                try
                {
                    var userManager = services.GetRequiredService<UserManager<DateTimeServiceUser>>();
                    var rolesManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var configuration = services.GetRequiredService<IConfiguration>();
                    await RoleInitializer.InitializeAsync(userManager, rolesManager, configuration);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }

                try
                {
                    var db = services.GetRequiredService<DateTimeServiceContext>();
                    await RoleInitializer.CleanTokensAsync(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while clearing the database.");
                }
            }

            await app.RunAsync();
        }
    }
}