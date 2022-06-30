using AuthLibrary.Data;
using GiftCertificateService.Data;
using GiftCertificateService.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GiftCertificateService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers(options => options.Filters.Add(typeof(Filters.ConnectionResetExceptionFilter)));
            builder.Services.AddCors();
            builder.Services.AddDbContext<DateTimeServiceContext>(
                options => options.UseSqlServer(builder.Configuration.GetConnectionString("DateTimeServiceContextConnection"))
                );

            builder.Services.AddDefaultIdentity<DateTimeServiceUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<DateTimeServiceContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Adding Jwt Bearer  
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
                    ValidateLifetime = true
                };
            });

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ILoadBalancing, LoadBalancing>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHsts();
            }

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

            app.Run();
        }
    }
}