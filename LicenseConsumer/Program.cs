using LicenseProxy;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;



var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000/") // Set this to your API's base address
};

var licenseClient = new LicenseClient(httpClient);
var licenseRequest = new LicenseRequest
{
    FeatureName = "ABC",
    FeatureVersion = "1.0",
    DeviceId = "XYZ1122"
};

var response = await licenseClient.IsLicensed(licenseRequest);
Console.WriteLine($"License Status: {(response.LicenseStatus == 1 ? "Licensed" : "Not Licensed")}");
