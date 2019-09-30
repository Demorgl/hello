using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Xml;
using Quorus.Cryptography.Signature;

namespace Base64Decode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly DESCryptoServiceProvider _provider = new DESCryptoServiceProvider();
        private readonly byte[] _key = { 95, 248, 105, 175, 160, 18, 164, 170 };

        public MainWindow()
        {
            InitializeComponent();
            TbSigName.Text = Properties.Settings.Default.CertName;
            TbSigX509.Text = Properties.Settings.Default.CertX509;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var result = Base64.Decode(TbIn.Text);

            TbOut.Text = result;
        }

        private void BCheckSignature_OnClick(object sender, RoutedEventArgs e)
        {
            tbValid.Text = null;

            if (string.IsNullOrEmpty(TbSignature.Text))
                return;

            const string signatureNamespace = "http://www.w3.org/2000/09/xmldsig#";

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(TbSignature.Text);
            }
            catch (Exception ex)
            {
                tbValid.Text = "Xml не валиден";
                return;
            }

            XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
            nsm.AddNamespace("pref", signatureNamespace);

            var signatures = doc.SelectNodes("//pref:Signature", nsm);
            if (signatures == null || signatures.Count == 0)
            {
                tbValid.Text = "Не найден элемент Signature";
                return;
            }

            foreach (var signature in signatures.Cast<XmlNode>())
            {
                if (!Verify(signature))
                {
                    tbValid.Text = $"Подпись не валидна в элементе {signature.SelectSingleNode("./pref:Object", nsm)?.FirstChild.LocalName}";
                    return;
                }
            }

            tbValid.Text = "Подпись верна";
        }

        private bool Verify(XmlNode document)
        {
            using (var stream = new MemoryStream())
            using (var reader = XmlWriter.Create(stream))
            {
                document.WriteTo(reader);
                reader.Flush();
                stream.Position = 0;
                var signedDocument = SignatureFactory.NewInstance(SignatureFactoryConstants.FactoryURI).NewSignedDocumentParser().Parse(stream);
                try
                {
                    return signedDocument.Verify() == 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            dbDecodePath.Text = string.Empty;

            if (string.IsNullOrEmpty(dbPath.Text))
                return;

            try
            {
                var d = Convert.FromBase64String(dbPath.Text);
                dbDecodePath.Text = Encoding.UTF8.GetString(_provider.CreateDecryptor(_key, _key).TransformFinalBlock(d, 0, d.Length));
            }
            catch (Exception)
            {
                dbDecodePath.Text = "Ошибка декодирования строки подключения";
            }
        }

        private void Sign1_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbSigName.Text) || string.IsNullOrEmpty(TbCleanXml.Text))
            {
                MessageBox.Show("Подпись или сообщение не задано");
                return;
            }

            try
            {
                Properties.Settings.Default.CertName = TbSigName.Text;
                Properties.Settings.Default.Save();

                var xml = new XmlDocument();
                xml.LoadXml(TbCleanXml.Text);

                var signer = new MqMessageSigner();
                //var signed = signer.SignMessageByCertName(xml.DocumentElement, TbSigName.Text);
                var signed = signer.SignMessageByCertNameNew(new StringReader(TbCleanXml.Text), TbSigName.Text);

                FormatXml(signed.OuterXml);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Ошибка при подписывании {exception}");
            }
        }

        private void Sign2_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbSigX509.Text) || string.IsNullOrEmpty(TbCleanXml.Text))
            {
                MessageBox.Show("Подпись или сообщение не задано");
                return;
            }

            Properties.Settings.Default.CertX509 = TbSigX509.Text;
            Properties.Settings.Default.Save();

            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(TbCleanXml.Text);

                var signer = new MqMessageSigner();
                var signed = signer.SignMessageByCert(xml.DocumentElement, TbSigX509.Text);
                

                FormatXml(signed.OuterXml);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Ошибка при подписывании {exception}");
            }
        }

        private void FormatXml(string xml)
        {
            using (MemoryStream mStream = new MemoryStream())
            using (XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.UTF8))
            {
                var document = new XmlDocument();
                document.LoadXml(xml);
                writer.Formatting = Formatting.Indented;
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                TbSigned.Text = formattedXml;
            }
        }
    }
}
