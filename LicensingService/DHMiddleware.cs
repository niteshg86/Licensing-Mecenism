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
                    KeySize=256,
                    SecretPrepend = Encoding.UTF8.GetBytes("ABC"),
                    SecretAppend = Encoding.UTF8.GetBytes("XYZ")
            };
                ecdh.GenerateKey(ECCurve.NamedCurves.nistP256);
               
                var publicKey = ecdh.PublicKey.ToByteArray();

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
                    var cngKey = CngKey.Import(Convert.FromBase64String(clientPublicKey), CngKeyBlobFormat.EccPublicBlob);
                    var clientPubKey = new ECDiffieHellmanCng(cngKey);
                     sharedSecret = ecdh.DeriveKeyMaterial(clientPubKey.PublicKey);

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
    }


}
