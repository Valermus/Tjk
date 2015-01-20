using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjkDesktop.Dto
{
    class HorseDto
    {
        public decimal kosuKodu { set; get; }
        public decimal kosuNo { set; get; }
        public int atId { set; get; }
        public string atYasi { set; get; }
        public int atSiraNo { set; get; }
        public string atKilo { set; get; }
        public string atAdi { set; get; }
        public string atBabaAdi { set; get; }
        public string atAnneAdi { set; get; }
        public string atJokey { set; get; }
        public string atSahip { set; get; }
        public string atAntrenor { set; get; }
        public string atStartNo { set; get; }
        public string atHandikapKum { set; get; }
        public string atSon6Yaris { set; get; }
        public string atKosmadigiGunSayisi { set; get; }
        public string atS20 { set; get; }
        public int atSonucNo { set; get; }
        public string atSonucDerece { set; get; }
        public string atSonucGanyan { set; get; }
        public string atSonucFark { set; get; }
        public string atSonucGecCikis { set; get; }
        public DateTime birthDate { set; get; }
        public List<HorseInfoDto> horseDetails { set; get; }
    }
}
