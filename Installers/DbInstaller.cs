using AuthLibrary.Data;
using Microsoft.EntityFrameworkCore;

namespace GiftCertificateService.Installers
{
    public class DbInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DateTimeServiceContext>(
                options => options.UseSqlServer(configuration.GetConnectionString("DateTimeServiceContextConnection"))
                );
        }
    }
}
