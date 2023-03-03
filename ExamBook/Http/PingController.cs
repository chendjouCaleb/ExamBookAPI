using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace ExamBook.Http
{
    [Route("api/ping")]
    public class PingController
    {
        
        [HttpGet]
        public string Ping()
        {
            return DateTime.UtcNow.ToString(CultureInfo.CurrentCulture);
        }
    }
}