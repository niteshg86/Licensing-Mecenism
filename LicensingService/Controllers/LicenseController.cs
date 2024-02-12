using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LicensingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        [HttpPost("isLicensed")]
        public IActionResult IsLicensed([FromBody] LicenseRequest request)
        {
            // Your logic to determine if the feature is licensed
            bool isLicensed = true; // Example logic
            return Ok(isLicensed);
        }

        [HttpGet("getmachineName")]
        public string GetMachineName()
        {
            return "MCHDhd887"; // Example machine name
        }
    }

    public class LicenseRequest
    {
        public string ? FeatureName { get; set; }
        public string ? FeatureVersion { get; set; }
        public string ? DeviceId { get; set; }
    }
}
