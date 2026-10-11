using System.Data.Entity;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Models
{
    // Partial class để mở rộng context không chỉnh sửa file auto-generated
    public partial class QL_VEPHIM_DuLieu
    {
        public virtual DbSet<KHUYENMAI> KHUYENMAIs { get; set; }
    }
}
