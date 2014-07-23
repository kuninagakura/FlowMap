using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMap
{
    class personBlob
    {

            public float x;
            public float y;
            public int? ID;
            public double size;
            public personBlob(float x, float y, double size)
            {
                this.size = size;
                this.x = x;
                this.y = y;
                this.ID = null;
            }
            public void assignID(int id)
            {
                this.ID = id;
            }
        
    }
}
