using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Clock;


    internal sealed class DateTimeProvider : IDateTimeProvider
    {
       
        public DateTime UtcNow => DateTime.UtcNow;
    }
