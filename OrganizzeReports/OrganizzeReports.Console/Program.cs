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
            System.Console.WriteLine("Para obter sua apiKey acesse: https://app.organizze.com.br/configuracoes/api-keys");
            string apiKey = System.Console.ReadLine();

            //string email = "";
            //string name = "";
            //string apiKey = "";

            var apiAdapter = new OrganizzeAPIAdapter(name, email, apiKey);
            var excelServce = new ExcelService();
            var reportService = new ReportService(apiAdapter, excelServce);

            var isReportGenerated = false;
            var error = string.Empty;

            Task.Run(async () =>
            {
                try
                {
                    await reportService.GenerateCategoryReport();

                    isReportGenerated = true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            });

            int dotsCount = 0;

            while (!isReportGenerated && string.IsNullOrEmpty(error))
            {
                string dots = new string('.', dotsCount % 4); // ciclo de 4 pontos
                System.Console.Write($"\rProcessando o relatório{dots}   ");
                dotsCount++;
                Thread.Sleep(500); // aguarda 0.5 segundos entre cada iteração
            }

            if(string.IsNullOrEmpty(error))
            {
                System.Console.WriteLine("\rRelatório processado com sucesso!");
            }
            else
            {
                System.Console.WriteLine("\rErro ao processar o relatório!");
                System.Console.ReadLine();
            }

        }
    }
}
