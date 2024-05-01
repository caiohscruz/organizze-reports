using ClosedXML.Excel;

namespace OrganizzeReports.Console.Services.ExcelService
{
    public class ExcelService
    {
        public void GenerateExcelFile(string fileName, IEnumerable<SpreadSheet> spreadSheets)
        {
            var workbook = new XLWorkbook();

            foreach (var spreadSheet in spreadSheets)
            {
                AddSpreadSheet(workbook, spreadSheet.Items, spreadSheet.Name, spreadSheet.CurrencyColumns);
            }

            workbook.SaveAs(fileName);
        }

        private void AddSpreadSheet(XLWorkbook workbook, IEnumerable<object> items, string sheetName, IEnumerable<int>? currencyColumns = null)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);

            worksheet.Cell(1, 1).InsertTable(items);

            if (currencyColumns != null)
            {
                foreach (var column in currencyColumns)
                {
                    var amountColumn = worksheet.Column(column);
                    amountColumn.Style.NumberFormat.Format = "R$ #,##0.00";
                }
            }

            worksheet.Columns().AdjustToContents();
        }

    }
}
