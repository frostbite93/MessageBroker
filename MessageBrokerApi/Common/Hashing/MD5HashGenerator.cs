using System.Security.Cryptography;
using System.Text;

namespace MessageBrokerApi.Common.Hashing
{
    public class MD5HashGenerator : IHashGenerator
    {
        public string ComputeHash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}