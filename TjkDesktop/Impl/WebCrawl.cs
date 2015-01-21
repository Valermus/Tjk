using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using TjkDesktop.Dto;
using TjkDesktop.App_Data;
using System.Threading;
using TjkDesktop.Responses;
using System.ComponentModel;
using System.Windows.Threading;

namespace TjkDesktop.Impl
{
    class WebCrawl
    {
        private static List<HipodromDto> hipodroms;
        private static readonly string validationPath = "http://www.gevezecafe.com/Validation.html";

        public class UiReporter
        {
            public string hipodromName { get; set; }
            public int kosuNo { set; get; }
            public string atName { set; get; }
            public string status { set; get; }
            public double percentage { set; get; }
            public DateTime? detayTarih { set; get; }
            public int detayId { set; get; }
            public TjkDesktop.MainWindow.Phases phase { set; get; }
            public string toplamAt { set; get; }
        }

        #region Unused Initiation Method
        public static ResponseAuthantication Validate()
        {
            WebClient wClient = new WebClient();
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            ResponseAuthantication response = new ResponseAuthantication();
            try
            {
                doc.Load(wClient.OpenRead(validationPath), Encoding.UTF8);

                string[] resultSpl = doc.DocumentNode.InnerText.Split('=');

                int result = int.Parse(resultSpl[1]);
                /*Program can no longer be used */
                if (result == 0)
                {
                    response.status = Util.Enums.ErrorStatus.CantBeUsed;
                    response.message = "Program kullanılamaz!";
                    return response;
                }
                else
                {
                    response.status = Util.Enums.ErrorStatus.Success;
                    response.message = "Validation başarılı";
                    return response;
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse == null)
                {
                    //No connection
                    response.status = Util.Enums.ErrorStatus.NoConnection;
                    response.message = "Lütfen internet bağlantınızı kontrol edin, Internet bağlantısı olmadan veri alınamayacağı için programı kullanamazsınız!";
                    return response;
                }
                else if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    //Couldn't find Validation page, connect to developer
                    response.status = Util.Enums.ErrorStatus.ValidationNotFound;
                    response.message = "Validation dosyası bulunamadı! Lütfen yönetici ile görüşürün!";
                    return response;
                }
                else
                {
                    //Other unknown errors
                    response.status = Util.Enums.ErrorStatus.Failed;
                    response.message = ex.Message;
                    return response;
                }
            }
            catch (Exception other)
            {
                response.status = Util.Enums.ErrorStatus.Failed;
                response.message = other.Message;
                return response;
            }

        }
        #endregion
        /* Phase-1: Hipodromların cekilerek db'e eklenmesi */
        public static ResponseHipodrom InitializeHipodroms(object sender, string date)
        {
            ResponseHipodrom response = new ResponseHipodrom();
            try
            {
                hipodroms = getHipodromsByDate(sender, date);
                InsertHipodroms(sender, hipodroms);
            }
            catch (System.NullReferenceException e)
            {
                response.error = e;
            }
            catch (Exception ex)
            {
                response.error = ex;
            }
            return response;
        }
        /* Phase-2:Kosularin cekilerek db'e eklemesi*/
        public static int InitializeYarisProgrami(object sender)
        {
            try
            {
                setYarisProgrami(sender, hipodroms);
                deleteYabanciYarislar(sender);
                insertKosular(sender, hipodroms);
            }
            catch (Exception)
            {
                return -1;
                throw;
            }
            return 1;
        }
        /*
        public static Task<ResponseHorseDetailSet> InitializeHorsesAndDetails()
        {
            return Task.Factory.StartNew(() =>
            {
                ResponseHorseDetailSet response = new ResponseHorseDetailSet();
                //response = setHorseDetailsToHorses(hipodroms);
                //insertHorses(hipodroms);
                return response;
            });
        }
        */
        public static ResponseHorseDetailSet InitializeHorsesAndDetails(object sender)
        {
            ResponseHorseDetailSet response = setHorseDetailsToHorses(sender, hipodroms);
            insertHorses(sender, hipodroms);
            return response;
        }
        public static int InitializeYarisSonuclari(object sender)
        {
            try
            {
                hipodroms = setYarisSonuclariToHipodroms(sender, hipodroms);
                insertKosular(sender, hipodroms);
                insertHorses(sender, hipodroms);
                insertHorseDetails(sender, hipodroms);
            }
            catch (System.Net.WebException)
            {
                return -1;
            }
            return 1;
        }

        private static void setYarisProgrami(object sender, List<HipodromDto> hipodromList)
        {
            hipodroms = setYarisProgramiToHipodroms(sender, hipodromList);

        }
        private static void insertHorseDetails(object sender, List<HipodromDto> hipodromList)
        {
            TjkDataSet dateset = new TjkDataSet();
            TjkDataSet.AtDetayDataTable dTable = dateset.AtDetay;
            TjkDesktop.App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter dTableAdapter = new App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter();
            dTableAdapter.Connection.Open();
            dTableAdapter.Fill(dTable);
            int totalHorseNum = hipodromList.Sum(x => x.kosular.Sum(y => y.horseList.Count));
            //Hack dto.fail'dan gelindiğinde burasi z.horseDetails null geliyor. Kontrol et.
            int totalDetail = hipodromList.Sum(x => x.kosular.Sum(y => y.horseList.Sum(z => z.horseDetails.Count)));
            UiReporter obj = new UiReporter();
            int counter = 0;
            double total = 0.0;
            foreach (HipodromDto h in hipodromList)
            {
                foreach (KosuDto k in h.kosular)
                {
                    foreach (HorseDto horse in k.horseList)
                    {
                        obj.atName = horse.atAdi;
                        obj.detayId = horse.atId;
                        counter++;
                        obj.toplamAt = counter + "/" + totalHorseNum;
                        //Tüm detaylar aliniyor
                        List<TjkDataSet.AtDetayRow> details = dTable.Where(q => q.AtId == horse.atId).ToList();
                        foreach (HorseInfoDto dto in horse.horseDetails)
                        {
                            total += (double)1 / totalDetail * 100;
                            obj.phase = MainWindow.Phases.Phase3;
                            obj.detayTarih = dto.tarih;
                            double? dereceSaniye = null;
                            TjkDataSet.AtDetayRow row = dTable.Where(q => q.AtId == dto.atId).Where(t => t.Tarih.CompareTo(dto.tarih) == 0).FirstOrDefault();
                            if (row != null)
                            {
                                bool isChanged = false;
                                if (dto.mesafe != null)
                                {
                                    if (row.IsMesafeNull() || (!dto.mesafe.ToString().Equals(row.Mesafe)))
                                    {
                                        row.Mesafe = dto.mesafe.Value.ToString();
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.pist != null)
                                {
                                    if (row.IsPistNull() || (!dto.pist.Equals(row.Pist)))
                                    {
                                        row.Pist = dto.pist;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.pist != null)
                                {
                                    if (row.IsPistNull() || (!dto.pist.Equals(row.Pist)))
                                    {
                                        row.Pist = dto.pist;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.sonucSiraNo != null)
                                {
                                    if (row.IsSonucSiraNoNull() || !dto.sonucSiraNo.Equals(row.SonucSiraNo))
                                    {
                                        row.SonucSiraNo = dto.sonucSiraNo;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.derece != null)
                                {
                                    string[] lines = dto.derece.Split('.');

                                    if (lines.Length > 1)
                                    {
                                        dereceSaniye = double.Parse(lines[0]) * 60;
                                        dereceSaniye += double.Parse(lines[1]);
                                        if (lines.Length > 2)
                                        {
                                            dereceSaniye += double.Parse(lines[2]) / 100;
                                        }
                                        if (row.Is_Derece_Saniye_Null() || dereceSaniye != row._Derece_Saniye_)
                                        {
                                            row._Derece_Saniye_ = (double)dereceSaniye;
                                            isChanged = true;
                                        }
                                        else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                    }
                                    if (row.IsDereceNull() || !dto.derece.Equals(row.Derece))
                                    {
                                        row.Derece = dto.derece;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.kilo != null)
                                {
                                    if (row.IsKiloNull() || !dto.kilo.ToString().Equals(row.Kilo))
                                    {
                                        row.Kilo = dto.kilo.Value.ToString();
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.jokey != null)
                                {
                                    if (row.IsJokeyNull() || !dto.jokey.Equals(row.Jokey))
                                    {
                                        row.Jokey = dto.jokey;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.antrenor != null)
                                {
                                    if (row.IsAntrenorNull() || !dto.antrenor.Equals(row.Antrenor))
                                    {
                                        row.Antrenor = dto.antrenor;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.sahip != null)
                                {
                                    if (row.IsSahipNull() || !dto.sahip.Equals(row.Sahip))
                                    {
                                        row.Sahip = dto.sahip;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.ganyan != null)
                                {
                                    if (row.IsGanyanNull() || !dto.ganyan.Equals(row.Ganyan))
                                    {
                                        row.Ganyan = dto.ganyan;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.grup != null)
                                {
                                    if (row.IsGrupNull() || !dto.grup.Equals(row.Grup))
                                    {
                                        row.Grup = dto.grup;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.kosuCinsAdi != null)
                                {
                                    if (row.IsKosuCinsAdiNull() || !dto.kosuCinsAdi.Equals(row.KosuCinsAdi))
                                    {
                                        row.KosuCinsAdi = dto.kosuCinsAdi;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.handikapKum != null)
                                {
                                    if (row.IsHandikapKumNull() || !dto.handikapKum.Equals(row.HandikapKum))
                                    {
                                        row.HandikapKum = dto.handikapKum;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.handikapCim != null)
                                {
                                    if (row.IsHandikapCimNull() || !dto.handikapCim.Equals(row.HandikapCim))
                                    {
                                        row.HandikapCim = dto.handikapCim;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.atIkramiye != null)
                                {
                                    if (row.IsAtIkramiyeNull() || !dto.atIkramiye.Value.ToString().Equals(row.AtIkramiye))
                                    {
                                        row.AtIkramiye = dto.atIkramiye.Value.ToString();
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (dto.s20 != null)
                                {
                                    if (row.IsS20Null() || !dto.s20.Equals(row.S20))
                                    {
                                        row.S20 = dto.s20;
                                        isChanged = true;
                                    }
                                    else { obj.status = "Veritabanı kontrol ediliyor.."; }
                                }
                                if (isChanged)
                                {
                                    obj.status = "Veritabanı güncelleniyor..";
                                    dTableAdapter.Update(dTable);
                                }
                            }
                            //Insert
                            else
                            {
                                string[] lines = dto.derece.Split('.');

                                if (lines.Length > 1)
                                {
                                    dereceSaniye = double.Parse(lines[0]) * 60;
                                    dereceSaniye += double.Parse(lines[1]);
                                    if (lines.Length > 2)
                                    {
                                        dereceSaniye += double.Parse(lines[2]) / 100;
                                    }
                                }
                                dTableAdapter.Insert((int?)dto.atId, dto.tarih, horse.atAdi, dto.sehir, dto.mesafe.ToString(), dto.pist, dto.sonucSiraNo, dto.derece, dereceSaniye, dto.kilo.ToString(),
                                    dto.jokey, dto.antrenor, dto.sahip, dto.ganyan, dto.grup, dto.kosuNo, dto.kosuCinsAdi, dto.handikapKum, dto.handikapCim, dto.atIkramiye.ToString(), dto.s20);
                                obj.status = "Veritabanına yazılıyor..";

                            }
                            obj.percentage = total;
                            (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), obj);
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            dTable.AcceptChanges();
            dTableAdapter.Connection.Close();
        }

        private static void insertHorses(object sender, List<HipodromDto> hipodromList)
        {
            TjkDesktop.App_Data.TjkDataSetTableAdapters.AtTableAdapter aTableAdapter = new App_Data.TjkDataSetTableAdapters.AtTableAdapter();
            aTableAdapter.Connection.Open();
            TjkDataSet dataset = new TjkDataSet();
            TjkDataSet.AtDataTable aTable = dataset.At;
            aTableAdapter.Fill(aTable);
            double total = 0;
            int counter = 0;
            int totalHorseNum = hipodromList.Sum(x => x.kosular.Sum(y => y.horseList.Count));
            UiReporter obj = new UiReporter();
            foreach (HipodromDto h in hipodromList)
            {
                foreach (KosuDto k in h.kosular)
                {
                    foreach (HorseDto a in k.horseList)
                    {
                        obj.toplamAt = counter + "/" + totalHorseNum;
                        obj.atName = a.atAdi;
                        obj.detayId = a.atId;
                        obj.phase = MainWindow.Phases.Phase1;
                        TjkDesktop.App_Data.TjkDataSet.AtRow row;
                        row = aTable.Where(y => y.KosuKodu == k.kod).Where(q => q.AtId == a.atId).FirstOrDefault();
                        /*Update existing row*/
                        if (row != null)
                        {
                            bool isChanged = false;
                            if (!row.IsStartNoNull() && !row.StartNo.Equals(a.atStartNo))
                            {
                                row.StartNo = a.atStartNo;
                                isChanged = true;
                            }
                            else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            if (!row.IsSiraNoNull() && row.SiraNo != a.atSiraNo)
                            {
                                row.SiraNo = a.atSiraNo;
                                isChanged = true;
                            }
                            else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            if (!row.IsHandikapKumNull() && !row.HandikapKum.Equals(a.atHandikapKum))
                            {
                                row.HandikapKum = a.atHandikapKum;
                                isChanged = true;
                            }
                            else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            if (!row.IsSon6YarisNull() && !row.Son6Yaris.Equals(a.atSon6Yaris))
                            {
                                row.Son6Yaris = a.atSon6Yaris;
                                isChanged = true;
                            }
                            else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            if (!row.IsKosmadigiGunSayisiNull() && !row.KosmadigiGunSayisi.Equals(a.atKosmadigiGunSayisi))
                            {
                                row.KosmadigiGunSayisi = a.atKosmadigiGunSayisi;
                                isChanged = true;
                            }
                            else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            if (a.atSonucNo != 0)
                            {
                                if (row.IsSonucNoNull() || a.atSonucNo != row.SonucNo)
                                {
                                    row.SonucNo = a.atSonucNo;
                                    isChanged = true;
                                }
                                else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            }
                            if (a.atSonucDerece != null)
                            {
                                if (row.IsSonucDereceNull() || (!a.atSonucDerece.Equals(row.SonucDerece)))
                                {
                                    row.SonucDerece = a.atSonucDerece;
                                    isChanged = true;
                                }
                                else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            }
                            if (a.atSonucGanyan != null)
                            {
                                if (row.IsSonucGanyanNull() || (!a.atSonucGanyan.Equals(row.SonucGanyan)))
                                {
                                    row.SonucGanyan = a.atSonucGanyan;
                                    isChanged = true;
                                }
                                else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            }
                            if (a.atSonucFark != null)
                            {
                                if (row.IsSonucFarkNull() || (!a.atSonucFark.Equals(row.SonucFark)))
                                {
                                    row.SonucFark = a.atSonucFark;
                                    isChanged = true;
                                }
                                else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            }
                            if (a.atSonucGecCikis != null)
                            {
                                if (row.IsSonucGecCikisNull() || (!a.atSonucGecCikis.Equals(row.SonucGecCikis)))
                                {
                                    row.SonucGecCikis = a.atSonucGecCikis;
                                    isChanged = true;
                                }
                                else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            }
                            if (a.birthDate != null)
                            {
                                if (row.IsDogumTarihiNull() || a.birthDate.CompareTo(row.DogumTarihi) != 0)
                                {
                                    row.DogumTarihi = a.birthDate;
                                    isChanged = true;
                                }
                                else { obj.status = "Veritabanı kontrol ediliyor.."; }
                            }
                            if (isChanged)
                            {
                                obj.status = "Veritabanı güncelleniyor..";
                                aTableAdapter.Update(aTable);
                            }
                        }
                        /*Insert new row*/
                        else
                        {
                            obj.status = "Veritabanına yazılıyor..";
                            aTableAdapter.Insert((int?)a.atId, (int?)a.kosuKodu, a.atAdi, a.atYasi, a.atBabaAdi, a.atAnneAdi, a.atStartNo, a.atSiraNo, a.atHandikapKum, a.atSon6Yaris,
                                                            a.atKosmadigiGunSayisi, a.atSonucNo, a.atSonucDerece, a.atSonucGanyan, a.atSonucFark, a.atSonucGecCikis, a.birthDate);
                        }
                        total += (double)1 / (k.horseList.Count * h.kosular.Count * hipodromList.Count) * 100;
                        obj.percentage = total;
                        (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), obj);
                        Thread.Sleep(1);
                    }
                }
            }
            aTable.AcceptChanges();
            aTableAdapter.Connection.Close();
        }
        private static void InsertHipodroms(object sender, List<HipodromDto> hipodromList)
        {
            TjkDesktop.App_Data.TjkDataSetTableAdapters.HipodromTableAdapter hTableAdapter = new App_Data.TjkDataSetTableAdapters.HipodromTableAdapter();
            hTableAdapter.Connection.Open();
            TjkDataSet dataSet = new App_Data.TjkDataSet();
            TjkDataSet.HipodromDataTable hTable = dataSet.Hipodrom;
            hTableAdapter.Fill(hTable);

            double total = 0;

            foreach (HipodromDto dto in hipodromList)
            {
                UiReporter obj = new UiReporter();
                obj.hipodromName = dto.name;
                /*Existing row if any*/
                TjkDesktop.App_Data.TjkDataSet.HipodromRow row;
                row = hTable.Where(r => r.HipodromId == dto.id).FirstOrDefault();
                if (row != null)
                {
                    /*Update existing row*/
                    bool isChanged = false;
                    if (dto.name != null)
                    {
                        if (row.IsNameNull() && !row.Name.Equals(dto.name))
                        {
                            row.Name = dto.name;
                            isChanged = true;
                        }
                        else
                        {
                            obj.status = "Veritabanı kontrol ediliyor..";
                        }
                    }
                    if (isChanged)
                    {
                        obj.status = "Veritabanı güncelleniyor..";
                        hTableAdapter.Update(hTable);
                    }
                }
                /*Insert new row*/
                else
                {
                    obj.status = "Veritabanına yazılıyor..";
                    hTableAdapter.Insert(dto.name, dto.id);
                }
                total = (double)((double)(hipodromList.IndexOf(dto) + 1) / hipodromList.Count) * 100;
                obj.percentage = total;
                (sender as BackgroundWorker).ReportProgress((int)total, obj);
                Thread.Sleep(1);
            }
            hTable.AcceptChanges();
            hTableAdapter.Connection.Close();
        }
        private static void insertKosular(object sender, List<HipodromDto> hipodromList)
        {
            TjkDesktop.App_Data.TjkDataSetTableAdapters.KosuTableAdapter kTableAdapter = new App_Data.TjkDataSetTableAdapters.KosuTableAdapter();
            kTableAdapter.Connection.Open();
            TjkDataSet dataSet = new App_Data.TjkDataSet();
            TjkDataSet.KosuDataTable kTable = dataSet.Kosu;
            kTableAdapter.Fill(kTable);
            double total = 0;
            UiReporter obj = new UiReporter();
            foreach (HipodromDto hipodrom in hipodromList)
            {
                foreach (KosuDto kosu in hipodrom.kosular)
                {
                    obj.hipodromName = hipodrom.name;
                    obj.kosuNo = (int)kosu.no;
                    TjkDataSet.KosuRow row = kTable.Where(k => k.Kod == kosu.kod).FirstOrDefault();
                    if (row != null)
                    {
                        /*Update existing row*/
                        bool isChanged = false;
                        if (kosu.no != (decimal?)null)
                        {
                            if (row.IsNoNull() || row.No != kosu.no)
                            {
                                row.No = (int)kosu.no;
                                isChanged = true;
                            }
                            else
                            {
                                obj.status = "Veritabanı kontrol ediliyor..";
                            }
                        }
                        if (kosu.tarih != null)
                        {
                            if (row.IsTarihNull() || row.Tarih.CompareTo(kosu.tarih) != 0)
                            {
                                row.Tarih = kosu.tarih;
                                isChanged = true;
                            }
                            else
                            {
                                obj.status = "Veritabanı kontrol ediliyor..";
                            }
                        }
                        if (kosu.saat != null)
                        {
                            if (row.IsSaatNull() || !row.Saat.Equals(kosu.saat))
                            {
                                row.Saat = kosu.saat;
                                isChanged = true;
                            }
                            else
                            {
                                obj.status = "Veritabanı kontrol ediliyor..";
                            }
                        }
                        if (kosu.son800 != null)
                        {
                            if (row.IsSon800Null() || !row.Son800.Equals(kosu.son800))
                            {
                                row.Son800 = kosu.son800;
                                isChanged = true;
                            }
                            else
                            {
                                obj.status = "Veritabanı kontrol ediliyor..";
                            }
                        }
                        if (isChanged)
                        {
                            obj.status = "Veritabanı güncelleniyor..";
                            kTableAdapter.Update(kTable);
                        }
                    }
                    /*Insert new row*/
                    else
                    {
                        obj.status = "Veritabanına yazılıyor..";
                        kTableAdapter.Insert((int?)hipodrom.id, (int?)kosu.kod, (int?)kosu.no, hipodrom.tarih, kosu.saat, kosu.groupAdi, kosu.son800);
                    }
                    total += (double)1 / (hipodrom.kosular.Count * hipodromList.Count) * 100;
                    obj.percentage = total;
                    (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), obj);
                    Thread.Sleep(1);
                }
            }
            kTable.AcceptChanges();
            kTableAdapter.Connection.Close();
        }
        private static void deleteYabanciYarislar(object sender)
        {
            HipodromDto hipodrom = hipodroms.Where(h => h.kosular.Where(k => k.kod < 0).FirstOrDefault() != null).FirstOrDefault();
            //List<HipodromDto> hipodromList = hipodroms.Where(h => h.kosular.Where(k => k.kod < 0) != null).ToList();
            //List<HipodromDto> patates = hipodroms.Where(x => x.kosular.Where(y => y.kod < 0 && y.hipodromId == x.id) != null).ToList();

            double total = 0;
            if (hipodrom != null)
            {
                UiReporter obj = new UiReporter();
                obj.phase = MainWindow.Phases.Phase2;
                obj.status = "Yabancı Yarış Siliniyor...";
                obj.hipodromName = hipodrom.name;
                total = 100.00;
                obj.percentage = total;
                (sender as BackgroundWorker).ReportProgress((int)total, obj);
                hipodroms.Remove(hipodrom);
                Thread.Sleep(1);
            }
        }
        private static List<HipodromDto> setYarisSonuclariToHipodroms(object sender, List<HipodromDto> hipodromList)
        {
            UiReporter obj = new UiReporter();
            double total = 0;
            foreach (HipodromDto hipodrom in hipodromList)
            {
                obj.hipodromName = hipodrom.name;
                WebClient wClient = new WebClient();
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                if (hipodrom.kosular.FirstOrDefault().kod < 0)
                {
                    return hipodromList;
                }
                doc.Load(wClient.OpenRead("http://www.tjk.org" + hipodrom.yarisSonuclariUrl), Encoding.UTF8);

                string infoClass = "info-level2-Son800";
                var info = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", infoClass)).Descendants().ToList();

                string classToFind = "race-no";
                HtmlNode div = doc.DocumentNode.Descendants("div").Where(d => d.Attributes.Contains("id") && d.Attributes["id"].Value.Equals("dataDiv")).FirstOrDefault();
                var ul = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));
                var nodes = ul.Descendants("a").Where(x => !x.Id.Equals("")).ToList();
                foreach (HtmlNode node in nodes)
                {
                    string[] parts = node.InnerHtml.Trim().Split(' ');
                    decimal kosuNo = parts.FirstOrDefault().Equals("") ? -1 : Decimal.Parse(parts.FirstOrDefault());
                    string saat = parts[parts.Length - 1];
                    string id = node.Attributes["href"].Value.Substring(1, node.Attributes["href"].Value.Length - 1);
                    decimal kosuKodu = id.Equals("") ? 0 : Decimal.Parse(id);
                    /*Ilgili kosu aliniyor */
                    KosuDto kosu = hipodrom.kosular.Where(x => x.kod == kosuKodu).FirstOrDefault();

                    HtmlNode kosuDiv = doc.DocumentNode.SelectNodes(string.Format("//div[@id='{0}']", kosu.kod)).FirstOrDefault();
                    HtmlNode table = kosuDiv.Descendants("table").FirstOrDefault().Descendants("tbody").FirstOrDefault();
                    IEnumerable<HtmlNode> trNodes = table.Descendants("tr");

                    obj.kosuNo = (int)kosu.no;
                    foreach (HtmlNode n in trNodes)
                    {
                        int horseId = getNumericPart(n.Descendants("td").ToList()[2].Descendants("a").FirstOrDefault().GetAttributeValue("href", null).Split('=').Last());
                        string derece = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[9].InnerText.Trim(), "\r\n")[0];
                        string ganyan = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[10].InnerText.Trim(), "\r\n")[0];
                        string fark = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[12].InnerText.Trim(), "\r\n")[0];
                        string gecCikis = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[13].InnerText.Trim(), "\r\n")[0];
                        string sonucNo = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[1].InnerText.Trim(), "\r\n")[0];
                        kosu.horseList.Where(h => h.atId == horseId).FirstOrDefault().atSonucDerece = derece;
                        kosu.horseList.Where(h => h.atId == horseId).FirstOrDefault().atSonucGanyan = ganyan;
                        kosu.horseList.Where(h => h.atId == horseId).FirstOrDefault().atSonucFark = fark;
                        kosu.horseList.Where(h => h.atId == horseId).FirstOrDefault().atSonucGecCikis = gecCikis;
                        kosu.horseList.Where(h => h.atId == horseId).FirstOrDefault().atSonucNo = getNumericPart(sonucNo);

                        obj.status = "Siteden alınıyor..";
                        total += (double)1 / (trNodes.ToList().Count * nodes.Count * hipodromList.Count) * 100;
                        obj.percentage = total;
                        obj.phase = MainWindow.Phases.Phase2;
                        (sender as BackgroundWorker).ReportProgress((int)total, obj);
                        Thread.Sleep(1);
                    }
                    string son800 = System.Text.RegularExpressions.Regex.Split(info[nodes.IndexOf(node)].InnerText.Trim(), "\r\n")[0].Split(':')[1].Trim();
                    kosu.son800 = son800;
                }
            }
            return hipodromList;
        }
        private static List<HipodromDto> getHipodromsByDate(object sender, string date)
        {
            // Parsing daily hipodrom list schedule
            List<HipodromDto> hipodromList = new List<HipodromDto>();
            var web = new HtmlWeb();
            var doc = web.Load("http://www.tjk.org/TR/YarisSever/Info/Page/GunlukYarisProgrami?QueryParameter_Tarih=" + date);

            string classToFind = "gunluk-tabs";
            //var divs = doc.DocumentNode.SelectNodes("//div[@id='dataDiv']");
            var ul = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));

            try
            {
                var nodes = ul.Descendants("a").ToList();

                double total = 0;
                foreach (HtmlNode node in nodes)
                {
                    HipodromDto dto = new HipodromDto();
                    dto.id = int.Parse(getNumericPartAsStr(node.Attributes["data-sehir-id"].Value));
                    dto.tarih = DateTime.Parse(date);
                    if (dto.id < 0)
                    {
                        return hipodromList;
                    }
                    dto.name = node.Attributes["id"].Value;
                    dto.yarisProgramiUrl = "/TR/YarisSever/Info/Sehir/GunlukYarisProgrami?SehirId=" + dto.id + "&" + "QueryParameter_Tarih=" + date + "&SehirAdi=" + dto.name;
                    dto.yarisSonuclariUrl = "/TR/YarisSever/Info/Sehir/GunlukYarisSonuclari?SehirId=" + dto.id + "&" + "QueryParameter_Tarih=" + date + "&SehirAdi=" + dto.name;
                    hipodromList.Add(dto);
                    total = (double)((double)(nodes.IndexOf(node) + 1) / nodes.Count) * 100;
                    UiReporter obj = new UiReporter();
                    obj.status = "Siteden alınıyor..";
                    obj.hipodromName = dto.name;
                    obj.percentage = total;
                    (sender as BackgroundWorker).ReportProgress((int)total, obj);
                    Thread.Sleep(1);
                }
                return hipodromList;
            }
            catch (Exception)
            {
                throw;
            }

        }
        /* TJK Yaris Programindaki kosulari ve kosudaki atlarin bilgilerini aldigi hipodrom listesine gore ekleyerek geri dondurur */
        private static List<HipodromDto> setYarisProgramiToHipodroms(object sender, List<HipodromDto> hipodromList)
        {
            double total = 0;
            foreach (HipodromDto h in hipodromList)
            {
                List<KosuDto> kosuBilgileriList = new List<KosuDto>();
                WebClient wClient = new WebClient();
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

                doc.Load(wClient.OpenRead("http://www.tjk.org" + h.yarisProgramiUrl), Encoding.UTF8);
                //var web = new HtmlWeb();
                //var doc = web.Load("http://www.tjk.org" + h.url);

                string classToFind = "race-no";
                HtmlNode div = doc.DocumentNode.Descendants("div").Where(d => d.Attributes.Contains("id") && d.Attributes["id"].Value.Equals("dataDiv")).FirstOrDefault();

                var ul = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));
                var nodes = ul.Descendants("a").Where(x => !x.Id.Equals("")).ToList();
                foreach (HtmlNode node in nodes)
                {
                    KosuDto dto = new KosuDto();
                    dto.sehir = h.name;
                    dto.tarih = h.tarih;
                    string[] parts = node.InnerHtml.Trim().Split(' ');
                    dto.no = parts.FirstOrDefault().Equals("") ? -1 : Decimal.Parse(parts.FirstOrDefault());
                    dto.saat = parts[parts.Length - 1];
                    string id = node.Attributes["href"].Value.Substring(1, node.Attributes["href"].Value.Length - 1);
                    dto.kod = id.Equals("") ? 0 : Decimal.Parse(id);
                    dto.hipodromId = h.id;
                    // Yabanci yarislarin kosu kodlari negatif gelmektedir ve bunlar alinmayacaktir
                    if (dto.kod < 0)
                    {
                        /* NOT: Burasi hicbirsey doldurmadan da dondurulebilir. DAL'a bagli olarak degistirilebilecektir */
                        /* Yabanci kosunun numarasi, kodu ve sehir bilgileri girildikten sonra geri donduruluyor */
                        kosuBilgileriList.Add(dto);
                        //Hipodromlara kosular ekleniyor

                        return hipodromList;
                    }

                    HtmlNode kosuDiv = doc.DocumentNode.SelectNodes(string.Format("//div[@id='{0}']", dto.kod)).FirstOrDefault();
                    HtmlNode info = kosuDiv.Descendants("h3").Where(t => t.GetAttributeValue("class", "") == "race-config").FirstOrDefault();
                    string[] lines = System.Text.RegularExpressions.Regex.Split(info.InnerText.Trim(), ",");
                    dto.groupAdi = lines[1].Trim();
                    HtmlNode table = kosuDiv.Descendants("table").FirstOrDefault().Descendants("tbody").FirstOrDefault();
                    IEnumerable<HtmlNode> trNodes = table.Descendants("tr");
                    List<HorseDto> horseList = new List<HorseDto>();
                    foreach (HtmlNode n in trNodes)
                    {
                        UiReporter obj = new UiReporter();
                        obj.phase = MainWindow.Phases.Phase2;
                        HorseDto horse = new HorseDto();
                        //Sira No
                        horse.atSiraNo = getNumericPart(System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[1].InnerText.Trim(), "\r\n")[0]);
                        //At adi
                        horse.atAdi = n.Descendants("td").ToList()[2].Descendants("a").FirstOrDefault().InnerText.Trim();
                        //At ID
                        horse.atId = getNumericPart(n.Descendants("td").ToList()[2].Descendants("a").FirstOrDefault().GetAttributeValue("href", null).Split('=').Last());
                        //At yasi
                        horse.atYasi = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[3].InnerText.Trim(), "\r\n")[0];
                        //At baba adi
                        horse.atBabaAdi = n.Descendants("td").ToList()[4].Descendants("a").FirstOrDefault().InnerText.Trim();
                        //At ana adi
                        horse.atAnneAdi = n.Descendants("td").ToList()[4].Descendants("a").LastOrDefault().InnerText.Trim();
                        //At kilo
                        horse.atKilo = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[5].InnerText.Trim(), "\r\n")[0];
                        //At Jokey adi
                        horse.atJokey = n.Descendants("td").ToList()[6].Descendants("a").FirstOrDefault().InnerText.Trim();
                        horse.atJokey = horse.atJokey.Replace("&#214;", "Ö");
                        horse.atJokey = horse.atJokey.Replace("&#199;", "Ç");
                        horse.atJokey = horse.atJokey.Replace("&#220;", "Ü");
                        //At Sahip adi
                        horse.atSahip = n.Descendants("td").ToList()[7].Descendants("a").FirstOrDefault().InnerText.Trim();
                        horse.atSahip = horse.atSahip.Replace("&#214;", "Ö");
                        horse.atSahip = horse.atSahip.Replace("&#199;", "Ç");
                        horse.atSahip = horse.atSahip.Replace("&#220;", "Ü");
                        //At Antrenor adi
                        if (n.Descendants("td").ToList()[8].Descendants("a").FirstOrDefault() != null)
                        {
                            horse.atAntrenor = n.Descendants("td").ToList()[8].Descendants("a").FirstOrDefault().InnerText.Trim();
                        }
                        else
                        {
                            horse.atAntrenor = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[8].InnerText.Trim(), "\r\n")[0];
                        }
                        horse.atAntrenor = horse.atAntrenor.Replace("&#214;", "Ö");
                        horse.atAntrenor = horse.atAntrenor.Replace("&#199;", "Ç");
                        horse.atAntrenor = horse.atAntrenor.Replace("&#220;", "Ü");
                        //At Start no
                        string startNo = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[9].InnerText.Trim(), "\r\n")[0];
                        horse.atStartNo = getNumericPartAsStr(startNo);
                        //At HK
                        horse.atHandikapKum = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[10].InnerText.Trim(), "\r\n")[0];
                        //At Son 6
                        horse.atSon6Yaris = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[11].InnerText.Trim(), "\r\n")[0];
                        //At Kosmadigi gun sayisi
                        horse.atKosmadigiGunSayisi = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[12].InnerText.Trim(), "\r\n")[0];
                        //At S20
                        horse.atS20 = System.Text.RegularExpressions.Regex.Split(n.Descendants("td").ToList()[13].InnerText.Trim(), "\r\n")[0];
                        //Kosu Bilgileri ekleniyor
                        horse.kosuKodu = dto.kod;
                        horse.kosuNo = (decimal)dto.no;
                        horseList.Add(horse);

                        obj.status = "Siteden alınıyor..";
                        obj.hipodromName = h.name;
                        obj.kosuNo = (int)horse.kosuNo;
                        obj.atName = horse.atAdi;
                        total += (double)1 / (trNodes.ToList().Count * nodes.Count * hipodromList.Count) * 100;
                        obj.percentage = total;
                        (sender as BackgroundWorker).ReportProgress((int)Math.Round(total), obj);
                        Thread.Sleep(1);
                    }
                    dto.horseList = horseList;
                    kosuBilgileriList.Add(dto);
                    //Hipodromlara kosular ekleniyor
                    h.kosular = kosuBilgileriList;
                }
            }
            return hipodromList;
        }
        private static ResponseHorseDetailSet setHorseDetailsToHorses(object sender, List<HipodromDto> hipodromList)
        {
            ResponseHorseDetailSet response = new ResponseHorseDetailSet();
            UiReporter reporter = new UiReporter();
            BdayCarrierDto dto = new BdayCarrierDto();

            List<HorseInfoDto> horseDetails;
            int totalHorseNum = hipodromList.Sum(x => x.kosular.Sum(y => y.horseList.Count));
            int counter = 0;

            /* En son tarihli detay db'de var mı */
            TjkDataSet dateset = new TjkDataSet();
            TjkDataSet.AtDetayDataTable dTable = dateset.AtDetay;
            TjkDesktop.App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter dTableAdapter = new App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter();
            dTableAdapter.Connection.Open();
            dTableAdapter.Fill(dTable);
            TjkDataSet.AtDetayRow lastRow;
            /*DB'deki en son 3 kayıt alınıyor*/

            foreach (HipodromDto h in hipodromList)
            {
                foreach (KosuDto k in h.kosular)
                {
                    foreach (HorseDto horse in k.horseList)
                    {
                        counter++;
                        reporter.toplamAt = counter + "/" + totalHorseNum;
                        lastRow = dTable.Where(aa => aa.AtId == horse.atId).OrderByDescending(ab => ab.Tarih).FirstOrDefault();
                        dto = getHorseDetail(sender, horse.atId, reporter, lastRow);
                        if (dto.isFailed)
                        {
                            response.failedToRetrieveIds.Add(horse.atId.ToString());
                        }
                        else
                        {
                            horseDetails = dto.horseInfoList;
                            horse.horseDetails = horseDetails;
                            horse.birthDate = dto.birthDay;
                        }
                    }
                }
            }
            dTableAdapter.Connection.Close();
            return response;
        }
        public static ResponseHorseDetailSet getHorseDetailsFromList(object sender, List<string> horseIDList)
        {
            ResponseHorseDetailSet response = new ResponseHorseDetailSet();
            BdayCarrierDto dto = new BdayCarrierDto();
            List<HorseInfoDto> horseDetails = new List<HorseInfoDto>();
            UiReporter reporter = new UiReporter();
            /* En son tarihli detay db'de var mı */
            TjkDataSet dateset = new TjkDataSet();
            TjkDataSet.AtDetayRow lastRow;
            TjkDataSet.AtDetayDataTable dTable = dateset.AtDetay;
            TjkDesktop.App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter dTableAdapter = new App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter();
            dTableAdapter.Connection.Open();
            dTableAdapter.Fill(dTable);
            foreach (string horseId in horseIDList)
            {
                lastRow = dTable.Where(aa => aa.AtId == int.Parse(horseId)).OrderByDescending(ab => ab.Tarih).FirstOrDefault();
                dto = getHorseDetail(sender, getNumericPart(horseId), reporter, lastRow);
                if (dto.isFailed)
                {
                    response.failedToRetrieveIds.Add(horseId);
                }
                else
                {
                    //Hack geliştirme yapılmalı lambda ile 
                    foreach (HipodromDto h in hipodroms)
                    {
                        foreach (KosuDto k in h.kosular)
                        {
                            foreach (HorseDto horse in k.horseList)
                            {
                                if (horse.atId == int.Parse(horseId))
                                {
                                    horseDetails = dto.horseInfoList;
                                    horse.horseDetails = horseDetails;
                                    horse.birthDate = dto.birthDay;
                                }

                            }
                        }
                    }
                }
            }
            dTableAdapter.Connection.Close();
            return response;
        }
        public static BdayCarrierDto getHorseDetail(object sender, int horseID, UiReporter reporter, TjkDataSet.AtDetayRow lastRow)
        {
            double total = 0;
            var web = new HtmlWeb();
            BdayCarrierDto dto = new BdayCarrierDto();
            List<HorseInfoDto> horseInfoDtoList = new List<HorseInfoDto>();
            try
            {
                var doc = web.Load("http://www.tjk.org/TR/YarisSever/Query/ConnectedPage/AtKosuBilgileri?1=1&QueryParameter_AtId=" + horseID.ToString());

                String pageResult = doc.DocumentNode.InnerHtml.Substring(doc.DocumentNode.InnerHtml.IndexOf("alt="), 19).Substring(5, 14);
                if (pageResult.Substring(0, 3).Equals("404"))
                {
                    dto.isFailed = true;
                    reporter.status = "Siteden veri alınamadı..!";
                    (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), reporter);
                    Thread.Sleep(5);
                    return dto;
                }
                var nodes = doc.DocumentNode.SelectNodes("//div[@id='dataDiv']");
                var table = doc.DocumentNode.SelectNodes("//table[@id='queryTable']");

                string classToFind = "grid_6";
                var grid = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));
                var spans = grid.Descendants("span").ToList();

                string birthDaySpan = System.Text.RegularExpressions.Regex.Split(spans[5].InnerHtml.Trim(), "\r\n")[0];

                if (!String.IsNullOrEmpty(birthDaySpan))
                {
                    dto.birthDay = DateTime.Parse(birthDaySpan);
                }

                List<HtmlNode> x = doc.GetElementbyId("tbody0").Elements("tr").ToList();

                if (x.Count > 0)
                {
                    string[] qq = System.Text.RegularExpressions.Regex.Split(x[0].Elements("td").ToList()[0].InnerText.Trim(), "\r\n");
                    DateTime? lastDetail = qq[0].Trim().Equals("") ? (DateTime?)null : DateTime.Parse(qq[0].Trim());
                    reporter.phase = MainWindow.Phases.Phase3;

                    if (lastRow != null && lastDetail.Value.CompareTo(lastRow.Tarih) == 0)
                    {
                        reporter.status = "Veritabanında mevcut, atlanıyor...";
                        reporter.detayId = horseID;
                        reporter.detayTarih = lastDetail;
                        reporter.percentage = 99.00;
                        (sender as BackgroundWorker).ReportProgress(99, reporter);
                        Thread.Sleep(5);
                    }
                    else
                    {
                        foreach (HtmlNode node in x)
                        {
                            HorseInfoDto horse = new HorseInfoDto();
                            List<HtmlNode> s = node.Elements("td").ToList();
                            horse.atId = horseID;

                            for (int i = 0; i < s.Count; i++)
                            {
                                reporter.status = "Siteden alınıyor..";
                                string[] lines = System.Text.RegularExpressions.Regex.Split(s[i].InnerText.Trim(), "\r\n");
                                if (i == 0)
                                {
                                    horse.tarih = lines[0].Trim().Equals("") ? (DateTime?)null : DateTime.Parse(lines[0].Trim());
                                }

                                else if (i == 1)
                                {
                                    horse.sehir = lines[0].Trim();
                                }
                                else if (i == 2)
                                {
                                    horse.mesafe = lines[0].Trim().Equals("") ? (Decimal?)null : Decimal.Parse(lines[0].Trim());
                                }
                                else if (i == 3)
                                {
                                    horse.pist = lines[0].Trim();
                                }
                                else if (i == 4)
                                {
                                    horse.sonucSiraNo = lines[0].Trim();
                                }
                                else if (i == 5)
                                {
                                    horse.derece = lines[0].Trim();
                                }
                                else if (i == 6)
                                {
                                    horse.kilo = lines[0].Trim().Equals("") ? (Double?)null : Double.Parse(lines[0].Trim());
                                }
                                else if (i == 7)
                                {
                                    horse.jokey = lines[0].Trim();
                                }
                                else if (i == 8)
                                {
                                    horse.ganyan = lines[0].Trim();
                                }
                                else if (i == 9)
                                {
                                    horse.grup = lines[0].Trim();
                                }
                                else if (i == 10)
                                {
                                    horse.kosuNo = lines[0].Trim();
                                }
                                else if (i == 11)
                                {
                                    horse.kosuCinsAdi = lines[0].Trim();
                                }
                                else if (i == 12)
                                {
                                    horse.antrenor = lines[0].Trim();
                                }
                                else if (i == 13)
                                {
                                    horse.sahip = lines[0].Trim();
                                }
                                else if (i == 14)
                                {
                                    horse.handikapCim = lines[0].Trim();
                                }
                                else if (i == 15)
                                {
                                    horse.handikapKum = lines[0].Trim();
                                }
                                else if (i == 16)
                                {
                                    horse.atIkramiye = lines[0].Trim().Equals("") ? (Decimal?)null : Decimal.Parse(lines[0].Trim());
                                }
                                else if (i == 19)
                                {
                                    horse.s20 = lines[0].Trim();
                                }
                                total += (double)1 / (s.Count * x.Count) * 100;
                                reporter.detayId = horse.atId;
                                reporter.detayTarih = horse.tarih;
                                reporter.percentage = total;

                                (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), reporter);
                                Thread.Sleep(5);
                            }
                            horseInfoDtoList.Add(horse);
                        }
                        dto.horseInfoList = horseInfoDtoList;
                        return dto;
                    }
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse == null)
                {
                    //No connection
                }
                if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    //404 Not Found
                }
                dto.isFailed = true;
                reporter.status = "Siteden veri alınamadı..!";
                (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), reporter);
                Thread.Sleep(5);
                return dto;
            }
            /*No detail found*/
            dto.horseInfoList = horseInfoDtoList;
            return dto;
        }

        //Todo - UI'da farklı bir alan eklenecek, tek horseID ile tüm detayların çekilmesi sağlanacak
        public static BdayCarrierDto getAllHorseDetails(object sender, int horseID, UiReporter reporter)
        {
            double total = 0;
            var web = new HtmlWeb();
            var doc = web.Load("http://www.tjk.org/TR/YarisSever/Query/ConnectedPage/AtKosuBilgileri?1=1&QueryParameter_AtId=" + horseID.ToString());
            String pageResult = doc.DocumentNode.InnerHtml.Substring(doc.DocumentNode.InnerHtml.IndexOf("alt="), 19).Substring(5, 14);
            BdayCarrierDto dto = new BdayCarrierDto();
            //int counter = 10;
            if (pageResult.Substring(0, 3).Equals("404"))
            {
                //while (counter < 10)
                //{
                doc = web.Load("http://www.tjk.org/TR/YarisSever/Query/ConnectedPage/AtKosuBilgileri?1=1&QueryParameter_AtId=" + horseID.ToString());
                pageResult = doc.DocumentNode.InnerHtml.Substring(doc.DocumentNode.InnerHtml.IndexOf("alt="), 19).Substring(5, 14);
                if (pageResult.Substring(0, 3).Equals("404"))
                {
                    dto.isFailed = true;
                    reporter.status = "Siteden veri alınamadı..!";
                    (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), reporter);
                    Thread.Sleep(50);
                    //counter--;
                }
                else
                {
                    //break;
                }
                // }
            }

            TjkDataSet dateset = new TjkDataSet();
            TjkDataSet.AtDetayDataTable dTable = dateset.AtDetay;
            TjkDesktop.App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter dTableAdapter = new App_Data.TjkDataSetTableAdapters.AtDetayTableAdapter();
            dTableAdapter.Connection.Open();
            dTableAdapter.Fill(dTable);
            var nodes = doc.DocumentNode.SelectNodes("//div[@id='dataDiv']");
            var table = doc.DocumentNode.SelectNodes("//table[@id='queryTable']");

            List<HorseInfoDto> horseInfoDtoList = new List<HorseInfoDto>();

            string classToFind = "grid_6";
            var grid = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", classToFind));
            var spans = grid.Descendants("span").ToList();

            string birthDaySpan = System.Text.RegularExpressions.Regex.Split(spans[5].InnerHtml.Trim(), "\r\n")[0];

            if (!String.IsNullOrEmpty(birthDaySpan))
            {
                dto.birthDay = DateTime.Parse(birthDaySpan);
            }

            List<HtmlNode> x = doc.GetElementbyId("tbody0").Elements("tr").ToList();

            foreach (HtmlNode node in x)
            {
                reporter.phase = MainWindow.Phases.Phase3;

                HorseInfoDto horse = new HorseInfoDto();
                List<HtmlNode> s = node.Elements("td").ToList();
                horse.atId = horseID;

                for (int i = 0; i < s.Count; i++)
                {
                    reporter.status = "Siteden alınıyor..";
                    string[] lines = System.Text.RegularExpressions.Regex.Split(s[i].InnerText.Trim(), "\r\n");
                    if (i == 0)
                    {
                        horse.tarih = lines[0].Trim().Equals("") ? (DateTime?)null : DateTime.Parse(lines[0].Trim());
                    }
                    else if (i == 1)
                    {
                        horse.sehir = lines[0].Trim();
                    }
                    else if (i == 2)
                    {
                        horse.mesafe = lines[0].Trim().Equals("") ? (Decimal?)null : Decimal.Parse(lines[0].Trim());
                    }
                    else if (i == 3)
                    {
                        horse.pist = lines[0].Trim();
                    }
                    else if (i == 4)
                    {
                        horse.sonucSiraNo = lines[0].Trim();
                    }
                    else if (i == 5)
                    {
                        horse.derece = lines[0].Trim();
                    }
                    else if (i == 6)
                    {
                        horse.kilo = lines[0].Trim().Equals("") ? (Double?)null : Double.Parse(lines[0].Trim());
                    }
                    else if (i == 7)
                    {
                        horse.jokey = lines[0].Trim();
                    }
                    else if (i == 8)
                    {
                        horse.ganyan = lines[0].Trim();
                    }
                    else if (i == 9)
                    {
                        horse.grup = lines[0].Trim();
                    }
                    else if (i == 10)
                    {
                        horse.kosuNo = lines[0].Trim();
                    }
                    else if (i == 11)
                    {
                        horse.kosuCinsAdi = lines[0].Trim();
                    }
                    else if (i == 12)
                    {
                        horse.antrenor = lines[0].Trim();
                    }
                    else if (i == 13)
                    {
                        horse.sahip = lines[0].Trim();
                    }
                    else if (i == 14)
                    {
                        horse.handikapCim = lines[0].Trim();
                    }
                    else if (i == 15)
                    {
                        horse.handikapKum = lines[0].Trim();
                    }
                    else if (i == 16)
                    {
                        horse.atIkramiye = lines[0].Trim().Equals("") ? (Decimal?)null : Decimal.Parse(lines[0].Trim());
                    }
                    else if (i == 19)
                    {
                        horse.s20 = lines[0].Trim();
                    }
                    total += (double)1 / (s.Count * x.Count) * 100;
                    reporter.detayId = horse.atId;
                    reporter.detayTarih = horse.tarih;
                    reporter.percentage = total;

                    (sender as BackgroundWorker).ReportProgress((int)Math.Round(total, 8, MidpointRounding.AwayFromZero), reporter);
                    Thread.Sleep(1);
                }
                horseInfoDtoList.Add(horse);
            }
            dto.horseInfoList = horseInfoDtoList;
            return dto;
        }
        private static string getNumericPartAsStr(string text)
        {
            int n;
            bool isNumber = int.TryParse(text, out n);
            if (isNumber)
            {
                return n.ToString();
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    string result = text.Substring(0, text.Length - i);
                    isNumber = int.TryParse(result, out n);
                    if (isNumber)
                    {
                        return result;
                    }
                }
            }
            return text;
        }
        private static int getNumericPart(string text)
        {
            int n;
            bool isNumber = int.TryParse(text, out n);
            if (isNumber)
            {
                return n;
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    string result = text.Substring(0, text.Length - i);
                    isNumber = int.TryParse(result, out n);
                    if (isNumber)
                    {
                        return n;
                    }
                }
            }
            return n;
        }
    }
}
