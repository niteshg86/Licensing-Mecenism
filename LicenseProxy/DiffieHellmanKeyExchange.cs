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
        private ECDiffieHellmanCng dh;
        private bool disposedValue;

        public DiffieHellmanKeyExchange()
        {
            dh = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256,
                KeySize=256,
                SecretPrepend = Encoding.UTF8.GetBytes("ABC"),
               SecretAppend = Encoding.UTF8.GetBytes("XYZ")
        };
            dh.GenerateKey(ECCurve.NamedCurves.nistP256);
        }

        public byte[] PublicKey => dh.PublicKey.ToByteArray();

        public byte[] DeriveKey(byte[] otherPartyPublicKey)
        {
            using var otherPartyPubKey = CngKey.Import(otherPartyPublicKey, CngKeyBlobFormat.EccPublicBlob);
            return dh.DeriveKeyMaterial(otherPartyPubKey);
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
