using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.Services;

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
            var reportService = new ReportService(apiAdapter);

            Task.Run(async () =>
            {
                
                var transactions = await reportService.GetTransactions();

                
                // Use os resultados aqui
            }).Wait();

        }
    }
}
