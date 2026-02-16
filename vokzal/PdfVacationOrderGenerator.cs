using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace vokzal
{
    public static class PdfVacationOrderGenerator
    {
        public static string Generate(Employees employee, VacationBooking vacation)
        {
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orders");
            Directory.CreateDirectory(outputDir);

            var fileName = $"VacationOrder_{employee.EmployeeID}_{vacation.StartDate:yyyyMMdd}_{vacation.EndDate:yyyyMMdd}.pdf";
            var filePath = Path.Combine(outputDir, fileName);

            var title = "ПРИКАЗ О ПРЕДОСТАВЛЕНИИ ОТПУСКА";
            var body =
                $"Сотрудник: {employee.FullName}\\n" +
                $"Дата приема на работу: {employee.HireDate:dd.MM.yyyy}\\n" +
                $"Период отпуска: {vacation.StartDate:dd.MM.yyyy} - {vacation.EndDate:dd.MM.yyyy}\\n" +
                $"Основание: {vacation.Reason}\\n" +
                $"Дата приказа: {DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}";

            WriteSimplePdf(filePath, title, body);
            return filePath;
        }

        private static void WriteSimplePdf(string path, string title, string body)
        {
            var escapedTitle = EscapePdfText(title);
            var escapedBody = EscapePdfText(body.Replace("\\n", "\n"));
            var contentStream =
                "BT\n" +
                "/F1 14 Tf\n" +
                "50 780 Td\n" +
                $"({escapedTitle}) Tj\n" +
                "/F1 11 Tf\n" +
                "0 -30 Td\n" +
                $"({escapedBody}) Tj\n" +
                "ET\n";

            var pdf = new StringBuilder();
            pdf.AppendLine("%PDF-1.4");

            var offsets = new int[6];

            offsets[1] = pdf.Length;
            pdf.AppendLine("1 0 obj");
            pdf.AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
            pdf.AppendLine("endobj");

            offsets[2] = pdf.Length;
            pdf.AppendLine("2 0 obj");
            pdf.AppendLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            pdf.AppendLine("endobj");

            offsets[3] = pdf.Length;
            pdf.AppendLine("3 0 obj");
            pdf.AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
            pdf.AppendLine("endobj");

            offsets[4] = pdf.Length;
            pdf.AppendLine("4 0 obj");
            pdf.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            pdf.AppendLine("endobj");

            offsets[5] = pdf.Length;
            pdf.AppendLine("5 0 obj");
            pdf.AppendLine($"<< /Length {contentStream.Length} >>");
            pdf.AppendLine("stream");
            pdf.Append(contentStream);
            pdf.AppendLine("endstream");
            pdf.AppendLine("endobj");

            var xrefOffset = pdf.Length;
            pdf.AppendLine("xref");
            pdf.AppendLine("0 6");
            pdf.AppendLine("0000000000 65535 f ");
            for (var i = 1; i <= 5; i++)
            {
                pdf.AppendLine($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n ");
            }

            pdf.AppendLine("trailer");
            pdf.AppendLine("<< /Size 6 /Root 1 0 R >>");
            pdf.AppendLine("startxref");
            pdf.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
            pdf.AppendLine("%%EOF");

            File.WriteAllText(path, pdf.ToString(), Encoding.ASCII);
        }

        private static string EscapePdfText(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }
    }
}
