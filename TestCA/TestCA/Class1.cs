using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;

namespace TestCA
{
    public class MyViewModel : DispatcherObject, INotifyPropertyChanged
    {
        private readonly IEnumerable<Data> myData;
        private readonly ObservableCollection<DataViewModel> myCollection;
        private ListCollectionView myCollectionView;

        public event PropertyChangedEventHandler PropertyChanged;

        public MyViewModel(IEnumerable<Data> data)
        {
            myData = data;
            myCollection = new ObservableCollection<DataViewModel>();
        }

        public ListCollectionView MyCollection
        {
            get { return myCollectionView ?? CreateMyCollectionView(); }
        }

        public object ViewModelState { get; private set; }
        public object State { get; private set; }

        private ListCollectionView CreateMyCollectionView()
        {
            myCollectionView = new ListCollectionView(myCollection)
            {
                CustomSort = new MyComparer(),
                Filter = MyFilter
            };

            FetchData(myData, dataObject => myCollection.Add(new DataViewModel(dataObject)));

            return myCollectionView;
        }

        private bool MyFilter(object obj)
        {
            throw new NotImplementedException();
        }

        protected void FetchData<T>(IEnumerable<T> data, Action<T> fetch)
        {
            State = ViewModelState.Fetching;

            var dataEnumerator = data.GetEnumerator();

            Dispatcher.BeginInvoke(
                new Action<IEnumerator<T>, Action<T>>
                    (
                    FetchDataImpl
                    ),
                DispatcherPriority.Background,
                dataEnumerator, fetch
                );
        }

        private void FetchDataImpl<T>(IEnumerator<T> sourceEnumerator, Action<T> fetch)
        {
            if (!sourceEnumerator.MoveNext())
            {
                State = ViewModelState.Active;
                return;
            }

            fetch(sourceEnumerator.Current);

            Dispatcher.BeginInvoke(
                new Action<IEnumerator<T>, Action<T>>
                    (
                    FetchDataImpl
                    ),
                DispatcherPriority.Background,
                sourceEnumerator, fetch
                );
        }
    }

    internal class MyComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            throw new NotImplementedException();
        }
    }

    public class Data
    {
    }

    internal class DataViewModel
    {
        private Data dataObject;

        public DataViewModel(Data dataObject)
        {

            this.dataObject = dataObject;
        }
    }
}