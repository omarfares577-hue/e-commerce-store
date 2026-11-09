using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application
{
    public interface IEmailservices
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
