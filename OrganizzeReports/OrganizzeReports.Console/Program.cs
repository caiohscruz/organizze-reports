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

            //string email = "";
            //string name = "";
            //string apiKey = "";

            var apiAdapter = new OrganizzeAPIAdapter(name, email, apiKey);
            var excelServce = new ExcelService();
            var reportService = new ReportService(apiAdapter, excelServce);

            var isReportGenerated = false;
            var hasErrors = false;

            Task.Run(async () =>
            {
                try
                {
                    await reportService.GenerateCategoryReport();

                    isReportGenerated = true;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Erro ao gerar o relatório: {ex.Message}");
                    hasErrors = true;
                }
            });

            int dotsCount = 0;

            while (!isReportGenerated && !hasErrors)
            {
                string dots = new string('.', dotsCount % 4); // ciclo de 4 pontos
                System.Console.Write($"\rProcessando o relatório{dots}   ");
                dotsCount++;
                Thread.Sleep(500); // aguarda 0.5 segundos entre cada iteração
            }

            System.Console.WriteLine("\rRelatório processado com sucesso!");

        }
    }
}
