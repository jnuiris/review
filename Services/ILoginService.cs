using IPWorkbench.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPWorkbench.Services
{
    public interface ILoginService
    {
        Task<LoginResult> LoginAsync(string username, string password);
    }
}
