using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ЗПиД6_2_1
{
    /// <summary>
    /// Логика взаимодействия для Passphrase.xaml
    /// </summary>
    public partial class Passphrase : Window
    {
        public Passphrase()
        {
            InitializeComponent();
        }

        private void btEnter_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
