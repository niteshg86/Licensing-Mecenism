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
                using var ecdh = new ECDiffieHellmanCng
                {
                    KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                    HashAlgorithm = CngAlgorithm.Sha256,
                    
                };
                ecdh.KeySize = 256; // Set the key size (256, 384, or 521 bits for P-256, P-384, P-521 curves)
                                    // Set the SecretPrepend and SecretAppend if using Hash or Hmac for KDF
                ecdh.SecretPrepend = Encoding.UTF8.GetBytes("ABC");
                ecdh.SecretAppend = Encoding.UTF8.GetBytes("XYZ");

                var publicKey = ecdh.PublicKey.ToByteArray();

                // Add your public key to response header
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Add("Public-Key", Convert.ToBase64String(publicKey));
                    return Task.CompletedTask;
                });

                if (context.Request.Headers.TryGetValue("Public-Key", out var clientPublicKey))
                {
                    var cngKey = CngKey.Import(Convert.FromBase64String(clientPublicKey), CngKeyBlobFormat.EccPublicBlob);
                    var clientPubKey = new ECDiffieHellmanCng(cngKey);
                    var sharedSecret = ecdh.DeriveKeyMaterial(clientPubKey.PublicKey);

                    // Use sharedSecret for encryption/decryption
                }

                await _next(context);
            }
            else
            {
                await _next(context);
            }
        }
    }


}
