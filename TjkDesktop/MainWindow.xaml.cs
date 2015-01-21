using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using TjkDesktop.Impl;
using TjkDesktop.Responses;

namespace TjkDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        BackgroundWorker worker;
        public enum Phases
        {
            Phase1 = 1,
            Phase2 = 2,
            Phase3 = 3,
            Phase4 = 4
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeButtons();
            this.DataContext = this;
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            /* Check for validation */
            ResponseAuthantication response = WebCrawl.Validate();
            if (response.status == Util.Enums.ErrorStatus.Success)
            {
                dtPicker.IsEnabled = true;
                btnHipodrom.IsEnabled = true;
            }
            else
            {
                MessageBox.Show(response.message, response.status.ToString(), MessageBoxButton.OK, MessageBoxImage.Warning);
                dtPicker.IsEnabled = false;
                btnHipodrom.IsEnabled = false;
            }
        }
        private void InitializeButtons()
        {
            btnHipodrom.IsEnabled = true;
            btnYarisProg.IsEnabled = true;
            btnHorseDetails.IsEnabled = true;
            btnYarisSonuc.IsEnabled = true;
            btnHipodrom.Visibility = System.Windows.Visibility.Visible;
            btnYarisProg.Visibility = System.Windows.Visibility.Hidden;
            btnHorseDetails.Visibility = System.Windows.Visibility.Hidden;
            btnYarisSonuc.Visibility = System.Windows.Visibility.Hidden;
            lblResult.Content = "";
            lblStatus.Content = "";
            pbTextBlock.Visibility = System.Windows.Visibility.Hidden;
            progBar.Value = 0;
            progBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnHorseDetails.Visibility = System.Windows.Visibility.Visible;
            btnYarisProg.IsEnabled = false;
            lblResult.Content = "Work Completed";
            progBar.Value = 0;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progBar.Value = e.ProgressPercentage;
            lblResult.Content = "";
            lblStatus.Content = "";
            pbTextBlock.Text = String.Format("{0:0.0}", Math.Round((e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).percentage, 2, MidpointRounding.AwayFromZero).ToString() + "%");
            string kosuNo = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).kosuNo == 0 ? "" : (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).kosuNo.ToString();
            string hip = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).hipodromName;
            string atAdi = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).atName;
            string status = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).status;
            Phases phase = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).phase;
            string atOran = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).toplamAt;
            if (progBar.Value == 100)
            {
                lblStatus.Content = "Hazır";
            }
            else
            {
                lblStatus.Content = status;
            }
            if (phase == Phases.Phase3)
            {
                int detayId = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).detayId;
                string kosuTarihi = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).detayTarih.Value.ToShortDateString();
                lblResult.Content += detayId + " ID'li atın ";
                lblResult.Content += kosuTarihi + " tarihli verileri ";
                lblStatus.Content += " " + atOran;
            }
            else if (phase == Phases.Phase1)
            {
                int detayId = (e.UserState as TjkDesktop.Impl.WebCrawl.UiReporter).detayId;
                lblResult.Content += detayId + " ID'li ";
                lblResult.Content += atAdi + " isimli at bilgileri işleniyor";
            }
            else
            {
                lblResult.Content = hip + " hipodromu ";
                if (!String.IsNullOrWhiteSpace(kosuNo) || !String.IsNullOrEmpty(kosuNo))
                {
                    lblResult.Content += kosuNo + ". Koşu ";
                }
                if (!String.IsNullOrWhiteSpace(atAdi) || !String.IsNullOrEmpty(atAdi))
                {
                    lblResult.Content += "- " + atAdi;
                }
            }

        }
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] parameters = e.Argument as object[];

            if ((Phases)parameters[0] == Phases.Phase1)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    progBar.Value = 0; progBar.Visibility = System.Windows.Visibility.Visible;
                    pbTextBlock.Visibility = System.Windows.Visibility.Visible;
                }), null);
                ResponseHipodrom response = WebCrawl.InitializeHipodroms(sender, parameters[1] as string);

                if (response.error != null)
                {
                    if (response.error is NullReferenceException)
                    {
                        MessageBox.Show("Seçtiğiniz tarihte kayıt bulunamadı");
                    }
                    else if (response.error is System.Net.WebException)
                    {
                        MessageBox.Show("Seçtiğiniz tarih için yarış sonucu kaydı bulunamadı");
                    }
                    else
                    {
                        MessageBox.Show(response.error.Message);
                    }
                }
                else
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        btnYarisProg.Visibility = System.Windows.Visibility.Visible;
                        btnHipodrom.IsEnabled = false;
                        lblResult.Content = "Hipodrom Bilgileri Başarıyla İşlendi...";
                    }), null);
                }
            }
            else if ((Phases)parameters[0] == Phases.Phase2)
            {
                int result = 0;
                result = WebCrawl.InitializeYarisProgrami(sender);
                if (result == -1)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show("Seçtiğiniz tarih için yarış programı bulunamadı");
                    }), null);

                }
                else if (result == 1)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        btnHorseDetails.Visibility = System.Windows.Visibility.Visible; btnYarisProg.IsEnabled = false;
                        lblResult.Content = "Yarış Programı Başarıyla İşlendi...";
                    }), null);

                }
            }
            else if ((Phases)parameters[0] == Phases.Phase3)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    btnHorseDetails.IsEnabled = false;
                }), null);
                ResponseHorseDetailSet response = WebCrawl.InitializeHorsesAndDetails(sender);
                if (response.failedToRetrieveIds != null)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        while (response.failedToRetrieveIds.Count > 0)
                        {
                            string msg = string.Join(Environment.NewLine, response.failedToRetrieveIds);
                            if (MessageBox.Show("Bu ID'deki atların detaylarına ulaşılamamıştır: " + msg + "\n Tekrar denensin mi?", "Erişilemeyen At Detayları", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            {
                                response = WebCrawl.getHorseDetailsFromList(sender, response.failedToRetrieveIds);
                            }
                            else
                            {
                                MessageBox.Show("Bu ID'deki atların detaylarına ulaşılamamıştır:" + msg);
                                break;
                            }
                        }
                    }), null);

                }
                Dispatcher.Invoke(new Action(() =>
                {
                    btnYarisSonuc.Visibility = System.Windows.Visibility.Visible;
                    lblResult.Content = "At Detayları Başarıyla İşlendi...";
                }), null);

            }
            else if ((Phases)parameters[0] == Phases.Phase4)
            {
                int result = 0;
                Dispatcher.Invoke(new Action(() =>
                {
                    btnYarisSonuc.IsEnabled = false;
                    btnYarisSonuc.Content = "Bekleyiniz...";
                }), null);
                result = WebCrawl.InitializeYarisSonuclari(sender);
                if (result == -1)
                {
                    MessageBox.Show("Seçtiğiniz tarih için yarış sonucu kaydı bulunamadı");
                }
                else if (result == 1)
                {
                    Dispatcher.Invoke(new Action(() =>
                        {
                            btnYarisSonuc.Visibility = System.Windows.Visibility.Visible;
                            btnYarisProg.IsEnabled = false;
                            btnYarisSonuc.IsEnabled = false;
                            btnYarisSonuc.Content = "Yarış Sonuçlarını Getir";
                            lblResult.Content = "Yarış Sonuçları Başarıyla İşlendi...";
                        }), null);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            if (dtPicker.SelectedDate == null)
            {
                MessageBox.Show("Lütfen Tarih Giriniz!");
            }
            else
            {
                String date = dtPicker.SelectedDate.Value.ToShortDateString();
                if (date != null && !date.Equals(""))
                {
                    date = date.Replace(".", "/");
                }
                worker.WorkerReportsProgress = true;
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.ProgressChanged += worker_ProgressChanged;

                object paramObj = Phases.Phase1;
                object paramObj2 = date;

                object[] parameters = new object[] { paramObj, paramObj2 };

                worker.RunWorkerAsync(parameters);

                /*

                new Thread(
                    delegate()
                    {
                        response = WebCrawl.InitializeHipodroms(date);

                    }).Start();
                */

            }
        }

        private void btnYarisProg_Click(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += worker_ProgressChanged;

            object paramObj = Phases.Phase2;

            object[] parameters = new object[] { paramObj };

            worker.RunWorkerAsync(parameters);

            //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            //result = WebCrawl.InitializeYarisProgrami();
        }

        private void BtnHorseDetails_Click(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += worker_ProgressChanged;

            object paramObj = Phases.Phase3;
            object[] parameters = new object[] { paramObj };

            worker.RunWorkerAsync(parameters);
        }
        /*
        private async void BtnHorseDetails_Click_1(object sender, RoutedEventArgs e)
        {
            ResponseHorseDetailSet response = new ResponseHorseDetailSet();
            btnHorseDetails.IsEnabled = false;
            response = await WebCrawl.InitializeHorsesAndDetails();

            if (response.failedToRetrieveIds != null)
            {
                while (response.failedToRetrieveIds != null)
                {
                    string msg = string.Join(Environment.NewLine, response.failedToRetrieveIds);
                    if (MessageBox.Show("Bu ID'deki atların detaylarına ulaşılamamıştır:" + msg + "\nTekrar denensin mi?", "Erişilemeyen At Detayları", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        response = WebCrawl.getHorseDetailsFromList(sender, response.failedToRetrieveIds);
                    }
                    else
                    {
                        MessageBox.Show("Bu ID'deki atların detaylarına ulaşılamamıştır:" + msg);
                        break;
                    }
                }
            }
            btnYarisSonuc.Visibility = System.Windows.Visibility.Visible;
        }
        */
        private void btnYarisSonuc_Click(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += worker_ProgressChanged;

            object paramObj = Phases.Phase4;
            object[] parameters = new object[] { paramObj };

            worker.RunWorkerAsync(parameters);
        }

        private void dtPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            InitializeButtons();
        }
    }
}
