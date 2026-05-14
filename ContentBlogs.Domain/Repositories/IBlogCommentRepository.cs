using ContentBlogs.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories
{
    public  interface IBlogCommentRepository :IRepository<BlogComment,Guid>
    {
    }
}
