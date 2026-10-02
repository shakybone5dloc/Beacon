using Beacon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Beacon.Application.Abstractions;

namespace Beacon.Infrastructure.Data;

public class BeaconDbContext(DbContextOptions<BeaconDbContext> options) : BeaconDbContext(options), IBeaconDbContext
{

}