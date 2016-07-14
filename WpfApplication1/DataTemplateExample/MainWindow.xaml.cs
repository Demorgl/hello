using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DataTemplateExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        public void MethodForTest()
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string s = string.Empty;
            s = "1";
            Color c = ((SolidColorBrush)Rectangle.Fill).Color;
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            
            //...
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
