using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Domain.ValueObjects
{
    public sealed class DateRange : ValueObject
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        private DateRange() { }

        public DateRange(DateTime start, DateTime end)
        {
            if (end < start) throw new ArgumentException("End date must be after start date.");
            Start = start;
            End = end;
        }

        public int DurationInDays => (End - Start).Days;
        public bool Contains(DateTime date) => date >= Start && date <= End;
        public bool Overlaps(DateRange other) => Start < other.End && End > other.Start;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Start;
            yield return End;
        }
    }
}
