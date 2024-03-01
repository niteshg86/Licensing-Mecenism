using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LicenseProxy
{
    public class DiffieHellmanKeyExchange : IDisposable
    {
        private ECDiffieHellman? dh = null;
        private bool disposedValue;
        public byte[] publicKey;

        public DiffieHellmanKeyExchange()
        {
            dh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            publicKey = dh.PublicKey.ExportSubjectPublicKeyInfo();
        }



        public byte[] DeriveAesKey(byte[] otherPartyPublicKey, string prependSecret = "", string appendSecret = "", int keySize = 32)
        {
            //var otherPartyEcdhPublicKey = ECDiffieHellmanCngPublicKey.FromByteArray(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob);
            //var sharedSecret = dh.DeriveKeyMaterial(otherPartyEcdhPublicKey);

            // Create a new ECDiffieHellman object for the purpose of importing the public key
            using var ecdhForImport = ECDiffieHellman.Create();
            ecdhForImport.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);

            // Derive the shared secret using the imported public key
            byte[] sharedSecret = dh.DeriveKeyMaterial(ecdhForImport.PublicKey);

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dh?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
