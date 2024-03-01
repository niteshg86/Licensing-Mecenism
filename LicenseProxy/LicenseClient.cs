using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LicenseProxy
{
    public class LicenseClient
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5); // Cache expiration time

        public LicenseClient(HttpClient httpClient, IMemoryCache memoryCache=null)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        public async Task<LicenseStatusResponse> IsLicensed(LicenseRequest request)
        {
            
            // Generate a unique cache key based on the request parameters
            var cacheKey = $"LicenseStatus-{request.FeatureName}-{request.FeatureVersion}";

            // Try to get the response from cache
            //if (_memoryCache.TryGetValue(cacheKey, out LicenseStatusResponse cachedResponse))
            //{
            //    return cachedResponse; // Return the cached response if available
            //}

            using var dh = new DiffieHellmanKeyExchange();
            var publicKey = dh.publicKey;

            // If not in cache, make the API call
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add the public key to the request headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Public-Key", Convert.ToBase64String(publicKey));

            // Make the API call
            var response = await _httpClient.PostAsync("Api/License/IsLicensed", content);
            response.EnsureSuccessStatusCode();

            LicenseStatusResponse licenseStatusResponse = null;
            if (response.Headers.TryGetValues("Public-Key", out var serverPublicKeyValues))
            {
                var serverPublicKey = Convert.FromBase64String(serverPublicKeyValues.FirstOrDefault());
                var secret = dh.DeriveAesKey(serverPublicKey, "ABC", "XYZ", 32);

                // Read the encrypted response
                var encryptedResponse = await response.Content.ReadAsByteArrayAsync();
                var iv = new byte[16];
                // Decrypt the response using the derived secret
                var decryptedResponseJson = AesEncryptionHelper.DecryptStringFromBytes_Aes(encryptedResponse, secret,iv);

                // Deserialize the decrypted response JSON to an object
                //licenseStatusResponse = JsonSerializer.Deserialize<LicenseStatusResponse>(decryptedResponseJson);
                licenseStatusResponse = new LicenseStatusResponse
                {
                    LicenseStatus = decryptedResponseJson == "true" ? 1 : 0
                };

            }

            //var responseString = await response.Content.ReadAsStringAsync();
            //var licenseStatusResponse = new LicenseStatusResponse
            //{
            //    LicenseStatus = responseString == "true" ? 1 : 0
            //};

            // Store the response in cache
            //_memoryCache.Set(cacheKey, licenseStatusResponse, _cacheExpiration);

            return licenseStatusResponse;


        }
    }

    public class LicenseRequest
    {
        public string FeatureName { get; set; }
        public string FeatureVersion { get; set; }
        public string DeviceId { get; set; }
    }

    public class LicenseStatusResponse
    {
        public int LicenseStatus { get; set; }
    }
}
