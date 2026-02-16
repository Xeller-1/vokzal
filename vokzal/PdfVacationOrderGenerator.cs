using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Globalization;
using System.IO;

namespace vokzal
{
    public static class PdfVacationOrderGenerator
    {
        private const string OrganizationName = "АО «ЖД Вокзал»";
        private const string OrganizationAddress = "г. Москва, Привокзальная площадь, д. 1";
        private const string OrganizationCodes = "ОКПО 00000000, Форма по ОКУД 0301005 (Т-6)";

        public static string Generate(Employees employee, VacationBooking vacation)
        {
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orders");
            Directory.CreateDirectory(outputDir);

            var fileName = $"Prikaz_otpuska_{employee.EmployeeID}_{vacation.StartDate:yyyyMMdd}.pdf";
            var filePath = Path.Combine(outputDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var document = new Document(PageSize.A4, 56, 56, 56, 56);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                var regular = CreateUnicodeFont(12, Font.NORMAL);
                var bold = CreateUnicodeFont(13, Font.BOLD);
                var title = CreateUnicodeFont(14, Font.BOLD);
                var small = CreateUnicodeFont(10, Font.NORMAL);

                var frame = new PdfPTable(1) { WidthPercentage = 100f };
                frame.DefaultCell.BorderColor = BaseColor.LIGHT_GRAY;
                frame.DefaultCell.BorderWidth = 1f;
                frame.DefaultCell.Padding = 16f;

                var contentCell = new PdfPCell { Border = Rectangle.NO_BORDER, Padding = 0f };

                AddCentered(contentCell, OrganizationName, bold);
                AddCentered(contentCell, OrganizationAddress, regular);
                AddCentered(contentCell, OrganizationCodes, small);
                AddSpacer(contentCell, 10f);

                AddCentered(contentCell, "ПРИКАЗ (РАСПОРЯЖЕНИЕ)", title);
                AddCentered(contentCell, "о предоставлении отпуска работнику", regular);
                AddSpacer(contentCell, 10f);

                var orderNumber = $"№ ОТ-{vacation.CreatedAt:yyyyMMdd}-{employee.EmployeeID}";
                var orderDate = vacation.CreatedAt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

                var orderInfo = new PdfPTable(2) { WidthPercentage = 100f };
                orderInfo.SetWidths(new[] { 7f, 3f });
                orderInfo.AddCell(CreateBorderlessCell($"Номер документа: {orderNumber}", regular));
                orderInfo.AddCell(CreateBorderlessCell($"Дата: {orderDate}", regular, Element.ALIGN_RIGHT));
                contentCell.AddElement(orderInfo);
                AddSpacer(contentCell, 8f);

                AddDataRow(contentCell, "Фамилия, имя, отчество", employee.FullName, regular);
                AddDataRow(contentCell, "Должность", employee.Positions?.PositionName ?? "—", regular);
                AddDataRow(contentCell, "Дата приема на работу", employee.HireDate.ToString("dd.MM.yyyy"), regular);

                var vacationType = string.IsNullOrWhiteSpace(vacation.Reason)
                    ? "ежегодный оплачиваемый отпуск"
                    : vacation.Reason.Trim();
                AddDataRow(contentCell, "Вид отпуска", vacationType, regular);

                var days = (vacation.EndDate.Date - vacation.StartDate.Date).Days + 1;
                AddDataRow(contentCell, "Период отпуска",
                    $"с {vacation.StartDate:dd.MM.yyyy} по {vacation.EndDate:dd.MM.yyyy} ({days} календарных дней)", regular);

                AddSpacer(contentCell, 8f);
                contentCell.AddElement(new Paragraph("Основание: утвержденный график отпусков и заявление работника.", regular));
                AddSpacer(contentCell, 20f);

                contentCell.AddElement(CreateSignatureLine("Руководитель организации", regular));
                AddSpacer(contentCell, 8f);
                contentCell.AddElement(CreateSignatureLine("С приказом ознакомлен(а)", regular));
                AddSpacer(contentCell, 10f);

                contentCell.AddElement(new Paragraph("Документ оформлен с учетом требований ГОСТ Р 7.0.97-2016.", small)
                {
                    Alignment = Element.ALIGN_LEFT
                });

                frame.AddCell(contentCell);
                document.Add(frame);
                document.Close();
            }

            return filePath;
        }

        private static iTextSharp.text.Font CreateUnicodeFont(float size, int style)
        {
            var windowsArial = @"C:\Windows\Fonts\arial.ttf";
            var windowsTimes = @"C:\Windows\Fonts\times.ttf";
            var fallbackLinux = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";

            var fontPath = File.Exists(windowsArial)
                ? windowsArial
                : File.Exists(windowsTimes)
                    ? windowsTimes
                    : fallbackLinux;

            var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            return new iTextSharp.text.Font(baseFont, size, style, BaseColor.BLACK);
        }

        private static void AddCentered(PdfPCell cell, string text, iTextSharp.text.Font font)
        {
            cell.AddElement(new Paragraph(text, font) { Alignment = Element.ALIGN_CENTER });
        }

        private static void AddSpacer(PdfPCell cell, float size)
        {
            cell.AddElement(new Paragraph(" ") { SpacingAfter = size });
        }

        private static PdfPCell CreateBorderlessCell(string text, iTextSharp.text.Font font, int align = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = align,
                Padding = 0f
            };
        }

        private static void AddDataRow(PdfPCell cell, string label, string value, iTextSharp.text.Font font)
        {
            var table = new PdfPTable(1) { WidthPercentage = 100f, SpacingBefore = 2f };
            table.AddCell(new PdfPCell(new Phrase($"{label}: {value}", font))
            {
                Border = Rectangle.BOTTOM_BORDER,
                BorderWidthBottom = 0.8f,
                BorderColorBottom = BaseColor.GRAY,
                PaddingBottom = 6f,
                PaddingTop = 2f,
                PaddingLeft = 0f,
                PaddingRight = 0f
            });
            cell.AddElement(table);
        }

        private static PdfPTable CreateSignatureLine(string label, iTextSharp.text.Font font)
        {
            var table = new PdfPTable(3) { WidthPercentage = 100f };
            table.SetWidths(new[] { 35f, 40f, 25f });
            table.AddCell(CreateBorderlessCell(label, font));
            table.AddCell(new PdfPCell
            {
                Border = Rectangle.BOTTOM_BORDER,
                BorderWidthBottom = 0.8f,
                BorderColorBottom = BaseColor.GRAY,
                MinimumHeight = 18f
            });
            table.AddCell(CreateBorderlessCell("(подпись)", CreateUnicodeFont(10, Font.NORMAL), Element.ALIGN_RIGHT));
            return table;
        }
    }
}
