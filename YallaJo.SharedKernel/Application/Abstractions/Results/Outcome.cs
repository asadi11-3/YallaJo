using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Results
{
    public enum Outcome
    {
        Ok = 200,
        Created = 201,
        Invalid = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        Conflict = 409,
        ServerError = 500,
        Canceled = 499
    }
}
