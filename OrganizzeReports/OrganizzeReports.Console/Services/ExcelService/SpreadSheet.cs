namespace OrganizzeReports.Console.Services.ExcelService
{
    public class SpreadSheet
    {
        internal IEnumerable<int>? currencyColumns;

        public string Name { get; set; }
        public IEnumerable<object> Items { get; set; }
        public IEnumerable<int>? CurrencyColumns { get; set; }
    }
}
