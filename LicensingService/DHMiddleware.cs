namespace LicensingService
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    public class DHMiddleware
    {
        private readonly RequestDelegate _next;

        public DHMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/Api/License/IsLicensed"))
            {
                using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                byte[] publicKey = ecdh.PublicKey.ExportSubjectPublicKeyInfo();
                
                // Add your public key to response header
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Add("Public-Key", Convert.ToBase64String(publicKey));
                    return Task.CompletedTask;
                });

              

                // Intercept the response
                var originalBodyStream = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                // Derive the shared secret and encrypt the response
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var plainTextResponse = await new StreamReader(responseBodyStream).ReadToEndAsync();
                byte[] sharedSecret = null;
                if (context.Request.Headers.TryGetValue("Public-Key", out var clientPublicKey))
                {
                    sharedSecret = DeriveAesKey(ecdh, Convert.FromBase64String(clientPublicKey), "ABC", "XYZ", 32);

                     var iv = new byte[16];
                    var encryptedResponse = AesEncryptionHelper.EncryptStringToBytes_Aes(plainTextResponse, sharedSecret, iv);
                    // Reset the original response body and write the encrypted content
                    context.Response.Body = originalBodyStream;
                    context.Response.ContentLength = encryptedResponse.Length; // Ensure the content length is set to the encrypted data length
                    await context.Response.Body.WriteAsync(encryptedResponse, 0, encryptedResponse.Length);
                }
                               
            }
            else
            {
                await _next(context);
            }
        }

        // Derive the AES key using the shared secret and optional secrets
        private byte[] DeriveAesKey(ECDiffieHellman ecdh, byte[] otherPartyPublicKey, string prependSecret = "", string appendSecret = "", int keySize = 32)
        {
            //ECDiffieHellmanPublicKey otherPartyEcdhPublicKey;
            //var otherPartyEcdhPublicKey = ECDiffieHellmanCngPublicKey.FromByteArray(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob);
            //var sharedSecret = ecdh.DeriveKeyMaterial(otherPartyEcdhPublicKey);

            // Create a new ECDiffieHellman object for the purpose of importing the public key
            using var ecdhForImport = ECDiffieHellman.Create();
            ecdhForImport.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);

            // Derive the shared secret using the imported public key
            byte[] sharedSecret = ecdh.DeriveKeyMaterial(ecdhForImport.PublicKey);

            using (var sha256 = SHA256.Create())
            {
                var prependBytes = Encoding.UTF8.GetBytes(prependSecret);
                var appendBytes = Encoding.UTF8.GetBytes(appendSecret);
                var combinedBytes = new byte[prependBytes.Length + sharedSecret.Length + appendBytes.Length];

                Buffer.BlockCopy(prependBytes, 0, combinedBytes, 0, prependBytes.Length);
                Buffer.BlockCopy(sharedSecret, 0, combinedBytes, prependBytes.Length, sharedSecret.Length);
                Buffer.BlockCopy(appendBytes, 0, combinedBytes, prependBytes.Length + sharedSecret.Length, appendBytes.Length);

                var hash = sha256.ComputeHash(combinedBytes);
                var key = new byte[keySize];
                Buffer.BlockCopy(hash, 0, key, 0, keySize);
                return key;
            }
        }
    }


}
