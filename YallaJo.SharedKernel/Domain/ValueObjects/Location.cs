using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Domain.ValueObjects
{
    public sealed class Location : ValueObject
    {
        public decimal Latitude { get; }
        public decimal Longitude { get; }

        private Location() { }

        public Location(decimal latitude, decimal longitude)
        {
            if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
            if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
            Latitude = latitude;
            Longitude = longitude;
        }

        public double DistanceTo(Location other)
        {
            const double R = 6371;
            var dLat = ToRadians((double)(other.Latitude - Latitude));
            var dLon = ToRadians((double)(other.Longitude - Longitude));
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians((double)Latitude)) * Math.Cos(ToRadians((double)other.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
        }

        public override string ToString() => $"({Latitude}, {Longitude})";
    }
}
