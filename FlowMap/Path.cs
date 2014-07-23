using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FlowMap
{
    class Path
    {
         public List<PointF> points;
            public bool open;
            public int ID;
            public personBlob blob;
            public Path(int ID, personBlob blob)
            {
                this.ID = ID;
                this.blob = blob;
                this.open = false;
                this.points = new List<PointF>();

            }

            public void addPoint(PointF p)
            {
                this.points.Add(p);
                return;
            }
    }
}
