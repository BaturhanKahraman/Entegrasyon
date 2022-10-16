using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerManager _customerManager;

        public CustomersController(CustomerManager customerManager)
        {
            _customerManager = customerManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomersPage(string customerInfo,int pageIndex=0,int itemCount = 50)
        {
            var result = await _customerManager.GetCustomerDetailPageable(customerInfo,pageIndex,itemCount);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> AddCustomer(AddCustomerDto dto)
        {
            var result = await _customerManager.AddCustomer(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> GetCustomers(UpdateCustomerDto dto)
        {
            var result = await _customerManager.UpdateCustomer(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
