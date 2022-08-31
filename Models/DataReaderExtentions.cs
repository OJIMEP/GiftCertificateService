using Microsoft.Data.SqlClient;
using AutoMapper;

namespace GiftCertificateService.Models
{
    public static class DataReaderExtentions
    {
        public static async Task<IEnumerable<T>> MapTo<T>(this SqlDataReader dataReader, IMapper mapper)
        {
            var result = new List<T>();

            while (await dataReader.ReadAsync())
            {
                result.Add(mapper.Map<T>(dataReader));
            }

            return result;
        }
    }
}
