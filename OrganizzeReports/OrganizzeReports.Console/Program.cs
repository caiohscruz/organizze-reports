using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.Services;
using OrganizzeReports.Console.Services.ExcelService;

namespace OrganizzeReports.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Digite seu email:");
            string email = System.Console.ReadLine();

            System.Console.WriteLine("Digite seu nome:");
            string name = System.Console.ReadLine();

            System.Console.WriteLine("Digite sua apiKey:");
            string apiKey = System.Console.ReadLine();           

            var apiAdapter = new OrganizzeAPIAdapter(name, email, apiKey);
            var excelServce = new ExcelService();
            var reportService = new ReportService(apiAdapter, excelServce);

            Task.Run(async () =>
            {                
               await reportService.GenerateCategoryReport();                
            }).Wait();

        }
    }
}
