using GiftCertificateService.Logging;
using GiftCertificateService.Models;

namespace GiftCertificateService.Services
{
    public interface ICertService
    {
        Task<List<ResponseCertGet>> GetCertsInfoByListAsync(List<string> barcodes);

        void SetLogElement(ElasticLogElement logElement);
    }
}
