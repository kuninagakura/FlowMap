using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMap
{
    class Frame
    {
            public int t;
            public List<personBlob> blobs = new List<personBlob>();
            public Frame(int t)
            {
                this.t = t;
            }
            public void addBlob(personBlob b)
            {
                this.blobs.Add(b);
            }

            internal void removeBlobs(List<personBlob> newBlobs)
            {
                foreach (personBlob remove in newBlobs)
                {
                    for (int i = 0; i < this.blobs.Count; i++)
                    {
                        if (this.blobs[i].x == remove.x && this.blobs[i].y == remove.y)
                        {
                            this.blobs.Remove(this.blobs[i]);
                        }
                    }
                }
            }
        

    }
}
