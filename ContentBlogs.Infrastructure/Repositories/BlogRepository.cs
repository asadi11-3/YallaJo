using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories
{
    public  class BlogRepository(ContentBlogsDbContext context) : EfRepository<Blog,Guid>(context) , IBlogRepository
    {
    }
}
