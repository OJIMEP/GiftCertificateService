using GiftCertificateService.Contracts.V1.Responses;
using GiftCertificateService.Logging;

namespace GiftCertificateService.Services
{
    public interface ICertService
    {
        Task<List<CertGetResponse>> GetCertsInfoByListAsync(List<string> barcodes);

        ElasticLogElementDTO GetLog();
    }
}
