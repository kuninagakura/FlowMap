using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FlowMap
{
    class PathList
    {
        public List<Path> paths;
            public PathList()
            {
                this.paths = new List<Path>();
            }

            public List<Path> getOpenPaths()
            {
                List<Path> ret = new List<Path>();
                foreach (Path p in this.paths)
                {
                    if (p.open)
                        ret.Add(p);
                }
                return ret;
            }

            public void addPath(Path p)
            {
                this.paths.Add(p);
                return;
            }
            
            public void addPointToPath(int id, PointF point)
            {
                for (int i = 0; i < this.paths.Count; i++) {
                    if (this.paths[i].ID == id)
                        this.paths[i].addPoint(point);
                }
                return;
            }
            public void closePath(int id)
            {
                for (int i = 0; i < this.paths.Count; i++)
                {
                    if (this.paths[i].ID == id)
                        this.paths[i].open = false;
                }
            }
            public bool isOpen(int id)
            {
                for (int i = 0; i < this.paths.Count; i++)
                {
                    if (this.paths[i].ID == id)
                        return this.paths[i].open;
                }
                return false;
            }
    }
}
