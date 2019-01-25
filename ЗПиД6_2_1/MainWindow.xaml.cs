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

namespace ЗПиД6_2_1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool flag = false;
        public string file;
        private byte[] dataText;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void rbPhrase_Checked(object sender, RoutedEventArgs e)
        {
            pbKeyPhrase.Opacity = 1;
            pbKeyPhrase.IsEnabled = true;
            flag = true;
        }

        private void rbGeneration_Checked(object sender, RoutedEventArgs e)
        {
            if (flag)
            {
                pbKeyPhrase.Opacity = 0;
                pbKeyPhrase.IsEnabled = false;
                flag = false;
            }
        }

        private string OpenDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            openFileDialog.Title = "Выберите файл";
            openFileDialog.Filter = "Текстовые файлы|*.txt";
            file = openFileDialog.FileName;
            return file;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            tbEnOpenDialog.Text = OpenDialog();
            StreamReader sr = new StreamReader(file);
            dataText = Encoding.UTF8.GetBytes(sr.ReadToEnd());
        }

        private void btDeOpen_Click(object sender, RoutedEventArgs e)
        {
            tbDeOpenDialog.Text = OpenDialog();
            StreamReader sr = new StreamReader(file);
            dataText = Encoding.UTF8.GetBytes(sr.ReadToEnd());
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

        private void Encode_Click(object sender, RoutedEventArgs e)
        {
            Passphrase pass = new Passphrase();
            pass.Owner = this;
            pass.ShowDialog();
            SymmetricAlgorithm sa;
            byte[] iv = new byte[8];
            CipherMode mode;
            PaddingMode padding = PaddingMode.Zeros;
            int n = 0;
            if (rbEnAES.IsChecked == true)
            {
                sa = AesCryptoServiceProvider.Create();
                n = 32;
            }
            else
            {
                sa = TripleDESCryptoServiceProvider.Create();
                n = 24;
            }
            if (n == 32)
                iv = new byte[16];
            byte[] key = new byte[n];
            if (rbEnCBC.IsChecked == true)
                mode = CipherMode.CBC;
            else
            {
                if (rbEnCFB.IsChecked == true)
                    mode = CipherMode.CFB;
                else
                    mode = CipherMode.CTS;
            }
            byte[] entrophy = new byte[32];
          /*  RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(entrophy);*/
            SHA256 sha256 = new SHA256Cng();
            entrophy = sha256.ComputeHash(Encoding.UTF8.GetBytes(pass.pbPass.Password));//114 174 3
            byte[] salt = Encoding.UTF8.GetBytes("saltsaltsalt");
            // File.WriteAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\Энтропия.txt", entrophy);
            if (rbGeneration.IsChecked == true)
            {
                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(pass.pbPass.Password, salt, 10000);
                key = pbkdf2.GetBytes(n);
            }
            else
            {
                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(pbKeyPhrase.Password, salt, 10000);
                key = pbkdf2.GetBytes(n);
            }
            byte[] encrypted = Encrypt(dataText, sa, key, iv, mode, padding);
            using (
                FileStream fs = new FileStream(@"D:\OSU\ЗПиД\ЗПиД6\Шифр.txt",
                    FileMode.Create))
            {
                fs.Write(encrypted, 0, encrypted.Length);
            }
            byte[] keyTemp = Encoding.UTF8.GetBytes(pass.pbPass.Password);
          //  SHA256 sha256 = new SHA256Managed();
            keyTemp = sha256.ComputeHash(keyTemp);
            byte[] keyForkey = new byte[n];
            Array.Copy(keyTemp, keyForkey, n);
          //  byte[] en = Encrypt(key, sa, keyForkey, iv, mode, padding);
            byte[] en = ProtectedData.Protect(key, entrophy, DataProtectionScope.CurrentUser);
            using (
                FileStream fs_key = new FileStream(@"D:\OSU\ЗПиД\ЗПиД6\Пароль.txt",
                    FileMode.Create))
            {
                fs_key.Write(en, 0, en.Length);
            }
            MessageBox.Show("Выполнено!");
            tbEnOpenDialog.Text = "";
            rbGeneration.IsChecked = true;
            pbKeyPhrase.Password = "";
            rbGeneration_Checked(sender, e);
        }

        private void btDecode_Click(object sender, RoutedEventArgs e)
        {
            Passphrase pass = new Passphrase();
            pass.Owner = this;
            pass.ShowDialog();
            byte[] decrypted;
            using (FileStream fileStream = File.OpenRead(file))
            {
                decrypted = new byte[fileStream.Length];
                fileStream.Read(decrypted, 0, decrypted.Length);
            }
            SymmetricAlgorithm sa;

            CipherMode mode;
            PaddingMode padding = PaddingMode.None;
            byte[] keyT = Encoding.UTF8.GetBytes(pass.pbPass.Password);
            SHA256 sha256 = new SHA256Cng();
            keyT = sha256.ComputeHash(keyT);
            int n = 0;
            if (rbDeAES.IsChecked == true)
            {
                sa = AesCryptoServiceProvider.Create();
                n = 32;
            }
            else
            {
                sa = TripleDESCryptoServiceProvider.Create();
                n = 24;
            }
            byte[] key = new byte[n];
            Array.Copy(keyT, key, n);
            byte[] iv = new byte[8];
            if (n == 24)
                iv = new byte[8];
            else
                iv = new byte[16];
            if (rbEnCBC.IsChecked == true)
                mode = CipherMode.CBC;
            else
            {
                if (rbEnCFB.IsEnabled)
                    mode = CipherMode.CFB;
                else
                    mode = CipherMode.CTS;
            }
            byte[] entrophy = sha256.ComputeHash(Encoding.UTF8.GetBytes(pass.pbPass.Password));//File.ReadAllBytes(@"D:\OSU\ЗПиД\ЗПиД6\Энтропия.txt");//113 183 
                                                                                               // SHA256 sha256 = new SHA256Cng();
                                                                                               //  entrophy = sha256.ComputeHash(entrophy);
            using (FileStream fstream = File.OpenRead(@"D:\OSU\ЗПиД\ЗПиД6\Пароль.txt"))
            {
                byte[] keyTemp = new byte[fstream.Length];
                fstream.Read(keyTemp, 0, keyTemp.Length);
                key = ProtectedData.Unprotect(keyTemp, entrophy, DataProtectionScope.CurrentUser);
              //  keyT = Decrypt(temp, sa, key, iv, mode, padding);
            }
            //Array.Copy(keyT, key, n);
            byte[] decr = Decrypt(decrypted, sa, key, iv, mode, padding);
            string result = Encoding.UTF8.GetString(decr);
            File.WriteAllText(@"C:\Users\Nastya\Desktop\Университет\ЗПиД\ЗПиД6\Результат.txt", result, Encoding.UTF8);
            MessageBox.Show("Выполнено!");
            tbDeOpenDialog.Text = "";
        }
    }
}
