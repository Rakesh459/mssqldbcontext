using Microsoft.AspNetCore.Mvc;
using practice.DataContext.Models;
using practice.DataExtensions;
using projects.DataContext;

namespace projects.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly PracticeDbContext _practiceContext;


    public WeatherForecastController(ILogger<WeatherForecastController> logger, PracticeDbContext practiceDbContext)
    {
        _logger = logger;
        _practiceContext = practiceDbContext;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IActionResult Get()
    {
        List<EmployeeDTO> list = null;
         _practiceContext.LoadStoredProc("spGetEmployeeIdNames")
        .WithSqlParam("Id", 1)
        .WithSqlParam("Name", "test")
        .ExecuteStoredProc(x =>  list = x.ReadToList<EmployeeDTO>());     
        return Ok(list);

    }
}
