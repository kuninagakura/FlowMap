using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Media.Media3D;
using System.Drawing;
using System.Windows.Media;
namespace Utilities
{
    public class util
    {
        //converts rgba ints into a byte[] with rgba within range (0-255)
        public static byte[] convert_rgba(int r, int g, int b, int alpha)
        {
            byte[] vals = new byte[4];
            if (r < 0) vals[0] = 0;
            else if (r > 255) vals[0] = 255;
            else vals[0] = (byte)r;

            if (g < 0) vals[1] = 0;
            else if (g > 255) vals[1] = 255;
            else vals[1] = (byte)g;

            if (b < 0) vals[2] = 0;
            else if (b > 255) vals[2] = 255;
            else vals[2] = (byte)b;

            if (alpha < 0) vals[3] = 0;
            else if (alpha > 200) vals[3] = 200;
            else vals[3] = (byte)alpha;
            return vals;

        }

        //fixed size queue for rendering effects
        public class FixedSizeQueue<T>
        {
            readonly ConcurrentQueue<T> q = new ConcurrentQueue<T>();
            public int Size { get; private set; }
            public FixedSizeQueue(int size)
            {
                Size = size;
            }
            public List<T> getList()
            {
                return this.q.ToList();
            }
            public void Enqueue(T obj)
            {

                q.Enqueue(obj);
                lock (this)
                {
                    while (q.Count > Size)
                    {
                        T outobj;
                        q.TryDequeue(out outobj);
                    }
                }

            }
        }

        public static double floatDist(float x1, float y1, float x2, float y2)
        {
            double deltaX = x1 - x2;
            double deltaY = y1 - y2;
            //distance formula
            double val = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

            return val;
        }

        //calculates the determinant of the triangle
        public static double triangleDeterminant(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            return x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2);
        }

        //if (x1, y1)->(x2, y2)->(x3, y3) are ccw, then return 1, otherwise, return -1
        public static int ccw(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (triangleDeterminant(x1, y1, x2, y2, x3, y3) > 0)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        //calculates the Y value of the line intersection of line defined by (x1, y1) (x2, y2) and (x3, y3) (x4, y4)
        public static double lineIntersectionY(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            double top = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4));
            double bottom = ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            return top / bottom;
        }

        //calculates the X value of the line intersection of line defined by (x1, y1) (x2, y2) and (x3, y3)(x4, y4)
        public static double lineIntersectionX(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            double top = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4));
            double bottom = ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            return top / bottom;
        }

        //gets the intersect value of the line
        public static double getIntersect(double x1, double y1, double m1)
        {
            return y1 - (m1 * x1);
        }

        //gets the slope of the line
        public static double getSlope(double x1, double y1, double x2, double y2)
        {
            return (y2 - y1) / (x2 - x1);

        }

        public static GeometryModel3D meshShape(double x, double y, double s, SolidColorBrush color)
        {
            Point3DCollection _points = new Point3DCollection();
            _points.Add(new Point3D(x - s / 2, y - s / 2, 0));
            _points.Add(new Point3D(x, y + s / 2, 0));
            _points.Add(new Point3D(x + s / 2, y + s / 2, 0));

            Int32Collection Tris = new Int32Collection();
            Tris.Add(0);
            Tris.Add(1);
            Tris.Add(2);

            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions = _points;
            mesh.TriangleIndices = Tris;
            mesh.Normals.Add(new Vector3D(0, 0, -1));

            GeometryModel3D sheet = new GeometryModel3D();
            sheet.Geometry = mesh;
            sheet.Material = new DiffuseMaterial(color);

            return sheet;
        }

    }
}
