using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;

namespace TjkDesktop
{
    /// <summary>
    /// Interaction logic for LoadingScreen.xaml
    /// </summary>
    public partial class LoadingScreen : Window, INotifyPropertyChanged
    {
        private int numRecsLoaded = 0;

        public string HipodromName
        {
            get { return HipodromName; }
            protected set
            {
                HipodromName = value;
                RaisePropertyChanged("NumRecsLoaded");
                RaisePropertyChanged("RecsLoadedMessage");
            }
        }
        public string AtName
        {
            get { return AtName; }
            protected set
            {
                AtName = value;
                RaisePropertyChanged("NumRecsLoaded");
                RaisePropertyChanged("RecsLoadedMessage");
            }
        }
        public int KosuNo
        {
            get { return KosuNo; }
            protected set
            {
                KosuNo = value;
                RaisePropertyChanged("NumRecsLoaded");
                RaisePropertyChanged("RecsLoadedMessage");
            }
        }
        public int NumRecsLoaded
        {
            get { return numRecsLoaded; }
            protected set
            {
                numRecsLoaded = value;
                RaisePropertyChanged("NumRecsLoaded");
                RaisePropertyChanged("RecsLoadedMessage");
            }
        }

        private int totalNumRecs;
        public int TotalNumRecs
        {
            get { return totalNumRecs; }
            set
            {
                totalNumRecs = value;
                RaisePropertyChanged("TotalNumRecs");
            }
        }

        public string RecsLoadedMessage
        {
            //get { return string.Format("Hipodrom {0} - Kosu {1} - At {2} / {3} getiriliyor...", HipodromName, KosuNo, AtName, numRecsLoaded); }
            get { return string.Format("Kayıt {0} işleniyor...", numRecsLoaded); }
        }
        public LoadingScreen()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string propName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public void Begin()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (s, ea) =>
            {
                NumRecsLoaded++;
                if (NumRecsLoaded >= TotalNumRecs)
                {
                    timer.Stop();
                    this.Close();
                }

            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 5);  // 500 2/sec
            timer.Start();
        }
        public void BeginInsert()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (s, ea) =>
            {
                NumRecsLoaded++;
                if (NumRecsLoaded >= TotalNumRecs)
                {
                    timer.Stop();
                    this.Close();
                }

            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 5);  // 500 2/sec
            timer.Start();
        }
        /*
         * public void Begin()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (s, ea) =>
            {
                NumRecsLoaded++;
                if (NumRecsLoaded == TotalNumRecs)
                {
                    timer.Stop();
                    //this.Close();
                }

            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);  // 500 2/sec
            timer.Start();
        }
         */
    }
}
