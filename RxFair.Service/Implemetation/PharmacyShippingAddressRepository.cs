using RxFair.Service.Interface;
using RxFair.Data.DbContext;
using RxFair.Data.DbModel;
using RxFair.Service.Implemetation.BaseService;

namespace RxFair.Service.Implemetation
{
    public class PharmacyShippingAddressRepository : GenericRepository<PharmacyShippingAddress>, IPharmacyShippingAddressService
    {
        private readonly RxFairDbContext _context;
        public PharmacyShippingAddressRepository(RxFairDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
