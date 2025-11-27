using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Models
{
    public class MaterialScanResponse
    {
        public Material Material { get; set; }
        public int PointsEarned { get; set; }
        public string Message { get; set; }
        public string QrData { get; set; }
        public int Quantity { get; set; }
    }
}
