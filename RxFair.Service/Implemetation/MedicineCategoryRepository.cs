﻿using RxFair.Data.DbContext;
using RxFair.Data.DbModel;
using RxFair.Data.Extensions;
using RxFair.Data.Utility;
using RxFair.Dto.Dtos;
using RxFair.Dto.Enum;
using RxFair.Service.Implemetation.BaseService;
using RxFair.Service.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace RxFair.Service.Implemetation
{
   public class MedicineCategoryRepository : GenericRepository<MedicineCategoryMaster>, IMedicineCategoryService
    {
        private readonly RxFairDbContext _context;
        public MedicineCategoryRepository(RxFairDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<MedicineCategoryView>> GetMecineCategoryList(SqlParameter[] paraObjects)
        {
            var dataSet = await _context.GetQueryDatatableAsync(StoredProcedureList.GetMedicineCategoryList, paraObjects);
            return Common.ConvertDataTable<MedicineCategoryView>(dataSet.Tables[0]);
        }

      
    }
}
