using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Sales;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Business.Concrete
{
    public class DiscountVoucherManager
    {
        private readonly IDiscountVoucherDal _discountVoucherDal;
        private readonly ApplicationLogManager _applicationLogManager;
        private readonly IRandomGenerator _randomGenerator;
        private const int CodeLength = 6;

        public DiscountVoucherManager(ApplicationLogManager applicationLogManager, IDiscountVoucherDal discountVoucherDal, IRandomGenerator randomGenerator)
        {
            _applicationLogManager = applicationLogManager;
            _discountVoucherDal = discountVoucherDal;
            _randomGenerator = randomGenerator;
        }

        public async Task<bool> CodeExits(string code)
        {
            return await _discountVoucherDal.Exists(x => string.Equals(x.Code,code));
        }

        public async Task<DiscountVoucher> CreateDiscountVoucher(decimal amount)
        {
            var discountVoucher = new DiscountVoucher { Amount=amount};
            string code = _randomGenerator.GetRandomCode(CodeLength);
            while(await CodeExits(code))
                code = _randomGenerator.GetRandomCode(CodeLength);           
            discountVoucher.Code = code;
            await _discountVoucherDal.AddAsync(discountVoucher);
            return discountVoucher;
        }
    }
}
