using FluentValidation;

namespace GiftCertificateService.Installers
{
    public class ValidationInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatorsFromAssemblyContaining(typeof(Program));
            ValidatorOptions.Global.LanguageManager.Enabled = false;
        }
    }
}
