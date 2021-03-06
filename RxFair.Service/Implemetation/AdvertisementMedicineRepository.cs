using RxFair.Data.DbContext;
using RxFair.Data.DbModel;
using RxFair.Service.Implemetation.BaseService;
using RxFair.Service.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace RxFair.Service.Implemetation
{
  public  class AdvertisementMedicineRepository : GenericRepository<AdvertisementMedicine>, IAdvertisementMedicineService
    {
        private readonly RxFairDbContext _context;
        public AdvertisementMedicineRepository(RxFairDbContext context) : base(context)
        {
            _context = context;
        }

        
    }
}