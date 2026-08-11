using Microsoft.EntityFrameworkCore;

namespace SmartFactory.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
}
