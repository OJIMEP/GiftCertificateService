using AuthLibrary.Data;
using GiftCertificateService.Data;
using GiftCertificateService.Services;

namespace GiftCertificateService.Installers
{
    public class ScopesInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILoadBalancing, LoadBalancing>();
            services.AddScoped<ICertService, CertService>();
        }
    }
}
