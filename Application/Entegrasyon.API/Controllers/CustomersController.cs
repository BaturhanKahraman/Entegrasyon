using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
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
        public async Task<IActionResult> AddCustomer(CustomerAddDto dto)
        {
            var result = await _customerManager.AddCustomer(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateCustomer(UpdateCustomerDto dto)
        {
            var result = await _customerManager.UpdateCustomer(dto);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomersBySearch(string searchText)
        {
            var result = await _customerManager.GetCustomerBySearch(searchText);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var result = await _customerManager.GetCustomerById(id);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
