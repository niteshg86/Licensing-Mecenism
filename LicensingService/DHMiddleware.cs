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

                // Intercept the response
                var originalBodyStream = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                // Derive the shared secret and encrypt the response
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var plainTextResponse = await new StreamReader(responseBodyStream).ReadToEndAsync();
                var sharedSecret = ecdh.DeriveKeyMaterial(CngKey.Import(/* Other party's public key */, CngKeyBlobFormat.EccPublicBlob));
                var iv = new byte[16];
                var encryptedResponse = AesEncryptionHelper.EncryptStringToBytes_Aes(plainTextResponse, sharedSecret, iv);

                // Reset the original response body and write the encrypted content
                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = encryptedResponse.Length; // Ensure the content length is set to the encrypted data length
                await context.Response.Body.WriteAsync(encryptedResponse, 0, encryptedResponse.Length);
            }
            else
            {
                await _next(context);
            }
        }
    }


}
