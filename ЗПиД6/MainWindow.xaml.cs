using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ЗПиД6
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static byte[] publicKey, publicAndPrivateKeys;
        private static string file;
        private byte[] dataText;
        private int keySize = 1024;

        private static void RSAGenerateKeys(int keySize)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize);
            publicKey = rsa.ExportCspBlob(false);
            publicAndPrivateKeys = rsa.ExportCspBlob(true);
        }

        private static string OpenDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            openFileDialog.Title = "Выберите файл";
            openFileDialog.Filter = "Текстовые файлы|*.txt";
            file = openFileDialog.FileName;
            return file;
        }

        private void btDeOpen_Click(object sender, RoutedEventArgs e)
        {
            tbDeOpen.Text = OpenDialog();
            StreamReader sr = new StreamReader(file);
            dataText = Encoding.UTF8.GetBytes(sr.ReadToEnd());
            sr.Close();
        }

        private void btEnOpen_Click(object sender, RoutedEventArgs e)
        {
            tbEnOpen.Text = OpenDialog();
            StreamReader sr = new StreamReader(file);
            dataText = Encoding.UTF8.GetBytes(sr.ReadToEnd());
            sr.Close();
        }

        private static byte[] Encrypt(byte[] message, SymmetricAlgorithm sa, byte[] key, byte[] iv, CipherMode mode,
            PaddingMode padding)
        {
            sa.Key = key;
            sa.IV = iv;
            sa.Mode = mode;
            sa.Padding = padding;
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, sa.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(message, 0, message.Length);
            cs.Close();
            ms.Close();
            return ms.ToArray();
        }

        private static byte[] Decrypt(byte[] cyphertext, SymmetricAlgorithm sa, byte[] key, byte[] iv, CipherMode mode,
            PaddingMode padding)
        {
            sa.Key = key;
            sa.IV = iv;
            sa.Mode = mode;
            sa.Padding = padding;
            MemoryStream ms = new MemoryStream(cyphertext);
            CryptoStream cs = new CryptoStream(ms, sa.CreateDecryptor(), CryptoStreamMode.Read);
            byte[] buffer = new byte[cyphertext.Length];
            int readCount = cs.Read(buffer, 0, cyphertext.Length);
            byte[] result = new byte[readCount];
            Array.Copy(buffer, result, readCount);
            cs.Close();
            ms.Close();
            return result;
        }

        private static byte[] RSASign(byte[] message, byte[] privateAndPublicKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(privateAndPublicKey);
            //Console.WriteLine(rsa.SignatureAlgorithm);
            HashAlgorithm sha256 = HashAlgorithm.Create("SHA256");
            byte[] hashBytes = sha256.ComputeHash(message);
            return rsa.SignHash(hashBytes, "SHA256");
        }

        private static bool RSAVerify(byte[] message, byte[] signature, byte[] publicKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(publicKey);
            HashAlgorithm sha256 = HashAlgorithm.Create("SHA256");
            byte[] hashBytes = sha256.ComputeHash(message);
            return rsa.VerifyHash(hashBytes, "SHA256", signature);
        }

        public void Verify(byte[] key, byte[] text)
        {
            byte[] signature = File.ReadAllBytes((@"D:\OSU\ЗПиД\ЗПиД6\RSAПодпись.txt"));
            if (RSAVerify(text, signature, key))
            {
                MessageBox.Show("Верификация пройдена!");
                tbDeOpen.Text = "";
            }
            else
            {
                MessageBox.Show("Верификация не пройдена!");
                Close();
            }
        }

        private void btDecode_Click(object sender, RoutedEventArgs e)
        {
            Passphrase pass = new Passphrase();
            pass.Owner = this;
            pass.ShowDialog();
            byte[] iv = new byte[16];
            byte[] publicAndPrivate =
                File.ReadAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\RSAЗакрытый.txt");
            byte[] publicKey = File.ReadAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\RSAОткрытый.txt");//6 2 0 0 0 164
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            SHA256 sha256 = new SHA256Cng();
            if (rbDeSign.IsChecked == true)
                Verify(publicKey, dataText);
            else
            {
                byte[] temp = Encoding.UTF8.GetBytes(pass.pbPass.Password);
                byte[] key = sha256.ComputeHash(temp);
                temp = Decrypt(publicAndPrivate, aes, key, iv, CipherMode.CBC, PaddingMode.None);
                byte[] privateKey = new byte[596];
                byte[] data = File.ReadAllBytes((@"D:\OSU\ЗПиД\ЗПиД6\RSAШифр.txt"));
                if (rbSignAndDecode.IsChecked==true)
                    Verify(publicKey, data);
                byte[] text = new byte[data.Length - 128];
                Array.Copy(temp, privateKey, 596);
                byte[] AsymKey = new byte[128];
                byte[] SymKey;
                for (int i = data.Length - 128, j = 0; i < data.Length; i++, j++)
                    AsymKey[j] = data[i];
                Array.Copy(data, text, data.Length - 128);
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
                {
                    rsa.ImportCspBlob(privateKey);
                    SymKey = rsa.Decrypt(AsymKey, false);
                }
                byte[] DeText = Decrypt(text, aes, SymKey, iv, CipherMode.CBC, PaddingMode.None);
                string result = Encoding.UTF8.GetString(DeText);
                File.WriteAllText(@"D:\OSU\ЗПиД\ЗПиД6\RSAРезультат.txt", result,
                    Encoding.UTF8);
                MessageBox.Show("Выполнено!");
            }
        }

        public void Sign(byte[] text, byte[] key)
        {
            byte[] signBytes = RSASign(text, key);
            using (
                FileStream fs = new FileStream(@"D:\OSU\ЗПиД\ЗПиД6\RSAПодпись.txt",
                    FileMode.Create))
            {
                fs.Write(signBytes, 0, signBytes.Length);
            }
        }

        private void btEncode_Click(object sender, RoutedEventArgs e)
        {
            byte[] symmetricKey = new byte[32];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(symmetricKey);
            Passphrase pass = new Passphrase();
            pass.Owner = this;
            pass.ShowDialog();
            byte[] iv = new byte[16];
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            RSAGenerateKeys(keySize);
            if (rbEnSign.IsChecked == true)
                Sign(dataText, publicAndPrivateKeys);
            else
            {
                byte[] SymEncrypt = Encrypt(dataText, aes, symmetricKey, iv, CipherMode.CBC, PaddingMode.Zeros);
                byte[] AsymKey;
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
                {
                    rsa.ImportCspBlob(publicKey);
                    AsymKey = rsa.Encrypt(symmetricKey, false);
                }
                using (
                    FileStream fs = new FileStream(@"D:\OSU\ЗПиД\ЗПиД6\RSAШифр.txt",
                        FileMode.Create))
                {
                    fs.Write(SymEncrypt, 0, SymEncrypt.Length);
                }
                using (
                    FileStream fileStream = new FileStream(
                        @"D:\OSU\ЗПиД\ЗПиД6\RSAШифр.txt", FileMode.Append))
                {
                    fileStream.Write(AsymKey, 0, AsymKey.Length);
                }
                if (rbSignAndEncode.IsChecked == true)
                    Sign(File.ReadAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\RSAШифр.txt"), publicAndPrivateKeys);
            }
            byte[] ForKeysT = Encoding.UTF8.GetBytes(pass.pbPass.Password);
            SHA256 sha256 = new SHA256Cng();
            byte[] ForKeys = sha256.ComputeHash(ForKeysT);
            publicAndPrivateKeys = Encrypt(publicAndPrivateKeys, aes, ForKeys, iv, CipherMode.CBC, PaddingMode.Zeros);
            File.WriteAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\RSAЗакрытый.txt",
                publicAndPrivateKeys);
            File.WriteAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\RSAОткрытый.txt", publicKey);
            MessageBox.Show("Выполнено!");
            tbEnOpen.Text = "";
        }
    }
}
