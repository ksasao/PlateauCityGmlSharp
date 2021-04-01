using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    /// <summary>
    /// 非凸多角形の三角形分割
    /// https://sonson.jp/blog/2007/02/12/1/ 参照
    /// </summary>
    public class Triangulator
    {
        /// <summary>
        /// 多角形を三角形に分割します
        /// </summary>
        /// <param name="list">緯度経度高度の点列</param>
        /// <param name="startIndex">頂点インデックスの開始番号</param>
        /// <param name="origin">原点として扱う緯度経度高度</param>
        /// <returns>分割した三角形</returns>
        public (Vertex[],Triangle[]) Convert(Position[] list, int startIndex, Position origin)
        {
            Vertex[] points = new Vertex[list.Length];

            for (int i=0; i < points.Length; i++)
            {
                points[i] = new Vertex { Index = i + startIndex, Value = list[i].ToVector3(origin) };
            }
            return (points,Convert(points));
        }
        /// <summary>
        /// 多角形を三角形に分割します
        /// </summary>
        /// <param name="pointList">点列</param>
        /// <returns>分割した三角形</returns>
        public Triangle[] Convert(Vector3[] vector3List)
        {
            Vertex[] pointList = ConvertToPointList(vector3List);
            return Convert(pointList);
        }
        /// <summary>
        /// 多角形を三角形に分割します
        /// </summary>
        /// <param name="pointList">点列</param>
        /// <returns>分割した三角形</returns>
        public Triangle[] Convert(Vertex[] pointList)
        {
            List<Triangle> triangles = new List<Triangle>();
            Vertex o = pointList[0]; // 3次元空間中の平面上に基準点を設定すべきだがこれで代用
            do
            {
                int index = FindMaxDistanceIndex(o, pointList);
                Vector3 cross = GetCrossProductAt(index, pointList);
                bool isValid = IsValid(index, pointList);
                int len = pointList.Length;
                if (isValid)
                {
                    triangles.Add(GetTriangle(pointList, index));
                    pointList = RemoveAt(index, pointList);
                }
                else
                {
                    int loop = index;
                    do
                    {
                        // ループを検出したら強制終了
                        index = (index + 1) % len;
                        if (loop == index)
                        {
                            return triangles.ToArray();
                        }
                        float dir = Vector3.Dot(cross, GetCrossProductAt(index, pointList));
                        if (dir > 0)
                        {
                            isValid = IsValid(index, pointList);
                            if (isValid)
                            {
                                triangles.Add(GetTriangle(pointList,index));
                                pointList = RemoveAt(index, pointList);
                                break;
                            }
                        }
                    } while (!isValid);
                }
            } while (pointList.Length > 3);
            triangles.Add(GetTriangle(pointList, 1));
            return triangles.ToArray();
        }
        private Triangle GetTriangle(Vertex[] list, int centerIndex)
        {
            int len = list.Length;
            return new Triangle
            {
                P0 = list[(centerIndex - 1 + len) % len],
                P1 = list[centerIndex],
                P2 = list[(centerIndex + 1) % len],
            };
        } 

        private Vertex[] ConvertToPointList(Vector3[] list)
        {
            Vertex[] plist = new Vertex[list.Length];
            for(int i=0; i < list.Length; i++)
            {
                plist[i] = new Vertex { Value = list[i], Index = i };
            }
            return plist;
        }
        private int FindMaxDistanceIndex(Vertex o, Vertex[] pointList)
        {
            int index = 0;
            double max = double.MinValue;
            for(int i=0; i < pointList.Length; i++)
            {
                double dist = Vector3.DistanceSquared(o.Value, pointList[i].Value);
                if(dist > max)
                {
                    max = dist;
                    index = i;
                }
            }
            return index;
        }
        private Vector3 GetCrossProductAt(int index, Vertex[] pointList)
        {
            int len = pointList.Length;
            Vector3 v1 = pointList[(index + 1) % len].Value - pointList[index].Value;
            Vector3 v2 = pointList[index].Value - pointList[(index - 1 + len) % len].Value;
            return Vector3.Cross(v1,v2);
        }
        private bool HasSameNormalDirection(Vertex A, Vertex B, Vertex C, Vertex P)
        {
            Vector3 abp = Vector3.Cross(B.Value - A.Value, P.Value - B.Value);
            Vector3 bcp = Vector3.Cross(C.Value - B.Value, P.Value - C.Value);
            Vector3 cap = Vector3.Cross(A.Value - C.Value, P.Value - A.Value);
            float dir1 = Vector3.Dot(abp, bcp);
            float dir2 = Vector3.Dot(abp, cap);
            return  dir1 > 0 &&  dir2 > 0;
        }

        private bool IsValid(int index, Vertex[] pointList)
        {
            int len = pointList.Length;
            Vertex A = pointList[(index - 1 + len) % len];
            Vertex B = pointList[index];
            Vertex C = pointList[(index + 1) % len];

            // index の点とその前後の点を頂点とする三角形を除外した点列を作る
            Vertex[] targets = new Vertex[len - 3];
            int pos = 0;
            for(int i=0; i < len; i++)
            {
                if(i != index && i != (index+1)%len && i != (index - 1 + len)%len)
                {
                    targets[pos] = pointList[i];
                    pos++;
                }
            }
            // 除外した三角形の中に他の点が含まれていないかどうかを判定
            for (int i = 0; i < targets.Length; i++)
            {
                Vertex p = targets[i];
                if (HasSameNormalDirection(A, B, C, p))
                {
                    return false;
                }
            }
            return true;
        }
        private Vertex[] RemoveAt(int index, Vertex[] pointList)
        {
            int pos = 0;
            Vertex[] removed = new Vertex[pointList.Length - 1];
            for(int i=0; i < pointList.Length; i++)
            {
                if(i != index)
                {
                    removed[pos++] = pointList[i];
                }
            }
            return removed;
        }
    }
}
