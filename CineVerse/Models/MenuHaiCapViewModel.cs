using System.Collections.Generic;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Models
{
    public class MenuHaiCapViewModel
    {
        public List<THELOAI> TheLoais { get; set; }
        public List<RAPCHIEU> RapChieus { get; set; }

        public MenuHaiCapViewModel()
        {
            TheLoais = new List<THELOAI>();
            RapChieus = new List<RAPCHIEU>();
        }
    }
}

