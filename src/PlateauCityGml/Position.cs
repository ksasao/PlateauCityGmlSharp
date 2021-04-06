using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlateauCityGml
{
    public class Position : IEquatable<Position>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }

        public static Position[] ParseString(string positionString)
        {
            string[] items = positionString.Split(' ');
            int len = items.Length / 3 - 1;  // 始点と終点は同じ値なので終点を無視
            Position[] list = new Position[len];
            for (int i = 0; i < len; i++)
            {
                list[i] = new Position
                {
                    Latitude = Convert.ToDouble(items[i*3]),
                    Longitude = Convert.ToDouble(items[i*3+1]),
                    Altitude = Convert.ToDouble(items[i*3+2])
                };
            }
            return list;
        }
        public static Position GetLowerCorner(Position[] positions)
        {
            if(positions == null || positions.Length == 0)
            {
                return null;
            }
            double latitude  = positions.Min(c => c.Latitude);
            double longitude = positions.Min(c => c.Longitude);
            double altitude  = positions.Min(c => c.Altitude);
            return new Position(latitude, longitude, altitude);

        }
        public static Position GetUpperCorner(Position[] positions)
        {
            if (positions == null || positions.Length == 0)
            {
                return null;
            }
            double latitude  = positions.Max(c => c.Latitude);
            double longitude = positions.Max(c => c.Longitude);
            double altitude  = positions.Max(c => c.Altitude);
            return new Position(latitude, longitude, altitude);

        }
        public static Position operator+ (Position p1, Position p2)
        {
            return new Position(p1.Latitude + p2.Latitude, p1.Longitude + p2.Longitude, p1.Altitude + p2.Altitude);
        }
        public static Position operator- (Position p1, Position p2)
        {
            return new Position(p1.Latitude - p2.Latitude, p1.Longitude - p2.Longitude, p1.Altitude - p2.Altitude);
        }
        public static bool operator== (Position p1, Position p2)
        {
            if(p1 == null && p2 == null)
            {
                return true;
            }
            return p1.Latitude == p2.Latitude && p1.Longitude == p2.Longitude && p1.Altitude == p2.Altitude;
        }
        public static bool operator !=(Position p1, Position p2)
        {
            if(p1 != null || p2 != null)
            {
                return true;
            }
            return p1.Latitude != p2.Latitude || p1.Longitude != p2.Longitude || p1.Altitude != p2.Altitude;
        }
        /// <summary>
        /// 点Pまでの距離を求める。まず緯度・経度から地表での距離を求め、その後高度を考慮。
        /// </summary>
        /// <param name="p">点P</param>
        /// <returns>距離(m)</returns>
        public double DistanceTo(Position p)
        {
            double dist = Haversine(p.Latitude, p.Longitude, Latitude, Longitude);
            return Math.Sqrt(dist * dist + (p.Altitude - Altitude) * (p.Altitude - Altitude));
        }

        /// <summary>
        /// 指定した origin を原点とした位置をX,Y,Z(m)に変換します。
        /// </summary>
        /// <param name="origin">原点</param>
        /// <returns>左手系 Y-up (Xが東方向を正、Yが上方向を正、Zが北方向を正)</returns>
        public Vector3 ToVector3(Position origin)
        {
            Position xp = new Position(origin.Latitude, Longitude, origin.Altitude);
            Position yp = new Position(Latitude, origin.Longitude, origin.Altitude);
            return new Vector3 {
                X = -(float)(xp.DistanceTo(origin) * ((Longitude - origin.Longitude) >= 0 ? 1.0 : -1.0)),
                Y = (float)(Altitude - origin.Altitude),
                Z = (float)(yp.DistanceTo(origin) * ((Latitude - origin.Latitude) >= 0 ? 1.0 : -1.0))
            };
        }
        /// <summary>
        /// Haversineの式により2点間の距離を求める
        /// </summary>
        /// <param name="latitude1">緯度1(°)</param>
        /// <param name="longitude1">経度1(°)</param>
        /// <param name="latitude2">緯度2(°)</param>
        /// <param name="longitude2">経度2(°)</param>
        /// <returns>2点間の距離(m)</returns>
        private double Haversine(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            // https://ja.wikipedia.org/wiki/%E5%A4%A7%E5%86%86%E8%B7%9D%E9%9B%A2
            const double r = 6371009;
            double lat1 = latitude1  * Math.PI / 180.0;
            double lat2 = latitude2  * Math.PI / 180.0;
            double lon1 = longitude1 * Math.PI / 180.0;
            double lon2 = longitude2 * Math.PI / 180.0;

            double dlat = Math.Abs(lat1 - lat2)/2.0;
            double dlon = Math.Abs(lon1 - lon2)/2.0;
            double ds = 2.0 * Math.Asin(
                    Math.Sqrt(
                        Math.Sin(dlat) * Math.Sin(dlat)
                        + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dlon) * Math.Sin(dlon)
                    )
                );
            return r * ds;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Position);
        }

        public bool Equals(Position other)
        {
            return other != null &&
                   Latitude == other.Latitude &&
                   Longitude == other.Longitude &&
                   Altitude == other.Altitude;
        }

        public override int GetHashCode()
        {
            int hashCode = -586440342;
            hashCode = hashCode * -1521134295 + Latitude.GetHashCode();
            hashCode = hashCode * -1521134295 + Longitude.GetHashCode();
            hashCode = hashCode * -1521134295 + Altitude.GetHashCode();
            return hashCode;
        }

        public Position() { }
        public Position(double latitude, double longitude, double altitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }
        public override string ToString()
        {
            return $"<{Latitude}, {Longitude}, {Altitude}>";
        }
    }
}
