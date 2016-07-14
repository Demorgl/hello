using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.CommandWpf;

namespace WpfApplication1
{
    public class TestClass
    {
        public string Str { get; set; }

        public int Intt { get; set; }

        public RelayCommand<object> ShowMe => new RelayCommand<object>(ShowMeAll);

        private void ShowMeAll(object param)
        {
            var p = param as TextBox;
            MessageBox.Show(p?.Tag?.ToString()?? "Пусто");
        }
    }

    public class AllClass
    {
        public ObservableCollection<TestClass> Collection { get; set; }

        public AllClass ()
        {
            Collection = new ObservableCollection<TestClass>
            {
                new TestClass {Intt = 1, Str = "str1"},
                new TestClass {Intt = 0, Str = "str2"},
                new TestClass {Intt = 3, Str = "str11312"},
                new TestClass {Intt = 0, Str = "str2aaaa"}
            };
        }
    }
}
