using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using Quorus.Cryptography.Signature;
using Quorus.Cryptography.Unification;
using Quorus.Cryptography.Unification.Utils;

namespace Base64Decode
{
    public class MqMessageSigner 
    {
        private SignatureFactory _factory;

        private SignatureFactory Factory
        {
            get
            {
                try
                {
                    if (_factory == null)
                    {
                        _factory = SignatureFactory.NewInstance(SignatureFactoryConstants.FactoryURI);
                    }
                    return _factory;
                }
                catch (Exception e)
                {
                    throw new Exception("Ошибка инициализации системы криптографии", e);
                }
            }
        }

        public XmlElement SignMessageByCertName(XmlElement element, string certificateName)
        {
            try
            {
                var docSigner = Factory.NewDocumentSigner();

                docSigner.SetObject(element);
                docSigner.SetCertificateBytes(S_GetPublicKey(certificateName));

                var signedDoc = docSigner.GetSignedDocument();
                var signedDocSerializer = Factory.NewSignedDocumentSerializer();

                var xmlDoc = new XmlDocument();
                using (var signedStream = signedDocSerializer.GetSignedXmlDocument(signedDoc))
                {
                    xmlDoc.Load(signedStream);
                }
                return xmlDoc.DocumentElement;
            }
            catch (Exception e)
            {
                throw new Exception("Ошибка при подписывании сообщения", e);
            }
        }

        public XmlElement SignMessageByCert(XmlElement element, string certificate)
        {
            try
            {
                var docSigner = Factory.NewDocumentSigner();

                docSigner.SetObject(element);
                docSigner.SetCertificateBytes(Convert.FromBase64String(certificate));

                var signedDoc = docSigner.GetSignedDocument();
                var signedDocSerializer = Factory.NewSignedDocumentSerializer();

                var xmlDoc = new XmlDocument();
                using (var signedStream = signedDocSerializer.GetSignedXmlDocument(signedDoc))
                {
                    xmlDoc.Load(signedStream);
                }
                return xmlDoc.DocumentElement;
            }
            catch (Exception e)
            {
                throw new Exception("Ошибка при подписывании сообщения", e);
            }
        }

        public XmlElement SignMessageByCertNameNew(TextReader tReader, string certificateName)
        {
            SignatureFactory factory = SignatureFactory.NewInstance(SignatureFactoryConstants.FactoryURI);

            XmlTextReader xmlReader = new XmlTextReader(tReader);
            xmlReader.Normalization = true;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.Load(xmlReader);

            IDocumentSigner signer = factory.NewDocumentSigner(false);
            signer.SetObject(xmlDoc.DocumentElement);
            signer.SetCertificateBytes(S_GetPublicKey(certificateName));

            ISignedDocument doc = signer.GetSignedDocument();

            using (var signedStream = factory.NewSignedDocumentSerializer().GetSignedXmlDocument(doc))
            {
                xmlDoc.Load(signedStream);
            }
            return xmlDoc.DocumentElement;

        }


        public void CheckCertificate(string certificate)
        {
            S_GetPublicKey(certificate);
        }

        private byte[] S_GetPublicKey(string certName)
        {
            try
            {
                var cryptoProvider = ObjectsCache.GetCryptoProvider(new Hashtable());

                using (var certificateStoreParams = UnificationFactory.CreateCertificateStoreParams(CertificateStoreConstants.LocalCertificateStoreName))
                using (var certificateStore = UnificationFactory.GetCertificateStore(cryptoProvider, certificateStoreParams))
                using (var certificateParams = UnificationFactory.CreateCertificateParams(certName))
                using (var certificate = UnificationFactory.GetCertificate(certificateStore, certificateParams))
                using (var stream = MemoryManagment.CreateStream())
                {
                    certificate.ExportPublicKey(stream);
                    var arr = stream.ToArray();
                    var str = Convert.ToBase64String(arr);
                    return arr;
                }
            }
            catch (Exception e)
            {
                throw new CryptographicException("Ошибка при получении дыннх сертификата " + certName, e);
            }
        }
    }
}