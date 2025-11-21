using System.Collections.Generic;
using netDxf.Entities;

namespace Lens3DWinForms.Models
{
    public class PlaneData
    {
        public List<netDxf.Vector3> Points { get; set; }
        public Line Line1 { get; set; }
        public Line Line2 { get; set; }
        public double Area { get; set; }
    }
}