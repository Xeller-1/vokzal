using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Globalization;
using System.IO;

namespace vokzal
{
    public static class PdfVacationOrderGenerator
    {
        private const string OrganizationName = "АО «ЖД Вокзал»";
        private const string OrganizationAddress = "г. Москва, Привокзальная площадь, д. 1";
        private const string OrganizationCode = "ОКПО 00000000";
        private const string FormCode = "Форма по ОКУД 0301005 (Т-6)";

        public static string Generate(Employees employee, VacationBooking vacation)
        {
            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orders");
            Directory.CreateDirectory(outputDir);

            var fileName = $"Prikaz_otpuska_{employee.EmployeeID}_{vacation.StartDate:yyyyMMdd}.pdf";
            var filePath = Path.Combine(outputDir, fileName);

            using (var document = new PdfDocument())
            {
                document.Info.Title = "Приказ о предоставлении отпуска";
                document.Info.Author = OrganizationName;
                document.Info.Subject = "Кадровый документ";

                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                page.Orientation = PdfSharp.PageOrientation.Portrait;

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    gfx.DrawRectangle(XPens.LightGray, 36, 36, page.Width - 72, page.Height - 72);
                    var unicodeOptions = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always);

                    var headerFont = new XFont("Times New Roman", 12, XFontStyle.Bold, unicodeOptions);
                    var regularFont = new XFont("Times New Roman", 12, XFontStyle.Regular, unicodeOptions);
                    var titleFont = new XFont("Times New Roman", 14, XFontStyle.Bold, unicodeOptions);

                    var left = 56;
                    var right = page.Width - 56;
                    var width = right - left;
                    var y = 52d;

                    DrawCentered(gfx, OrganizationName, headerFont, left, y, width);
                    y += 18;
                    DrawCentered(gfx, OrganizationAddress, regularFont, left, y, width);
                    y += 18;
                    DrawCentered(gfx, OrganizationCode, regularFont, left, y, width);
                    y += 18;
                    DrawCentered(gfx, FormCode, regularFont, left, y, width);

                    y += 20;
                    gfx.DrawLine(XPens.Black, left, y, right, y);
                    y += 26;

                    DrawCentered(gfx, "ПРИКАЗ (РАСПОРЯЖЕНИЕ)", titleFont, left, y, width);
                    y += 18;
                    DrawCentered(gfx, "о предоставлении отпуска работнику", regularFont, left, y, width);

                    y += 24;
                    var orderNumber = $"№ ОТ-{vacation.CreatedAt:yyyyMMdd}-{employee.EmployeeID}";
                    var orderDate = vacation.CreatedAt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                    gfx.DrawString($"Номер документа: {orderNumber}", regularFont, XBrushes.Black, new XRect(left, y, width * 0.65, 20), XStringFormats.TopLeft);
                    gfx.DrawString($"Дата: {orderDate}", regularFont, XBrushes.Black, new XRect(left + width * 0.65, y, width * 0.35, 20), XStringFormats.TopRight);

                    y += 32;
                    DrawField(gfx, "Фамилия, имя, отчество", employee.FullName, regularFont, left, right, ref y);

                    var positionName = employee.Positions?.PositionName ?? "—";
                    DrawField(gfx, "Должность", positionName, regularFont, left, right, ref y);

                    DrawField(gfx, "Дата приема на работу", employee.HireDate.ToString("dd.MM.yyyy"), regularFont, left, right, ref y);

                    var vacationType = string.IsNullOrWhiteSpace(vacation.Reason)
                        ? "ежегодный оплачиваемый отпуск"
                        : vacation.Reason.Trim();
                    DrawField(gfx, "Вид отпуска", vacationType, regularFont, left, right, ref y);

                    var days = (vacation.EndDate.Date - vacation.StartDate.Date).Days + 1;
                    DrawField(gfx, "Период отпуска", $"с {vacation.StartDate:dd.MM.yyyy} по {vacation.EndDate:dd.MM.yyyy} ({days} календарных дней)", regularFont, left, right, ref y);

                    y += 10;
                    var basis = "Основание: утвержденный график отпусков и заявление работника.";
                    gfx.DrawString(basis, regularFont, XBrushes.Black, new XRect(left, y, width, 20), XStringFormats.TopLeft);

                    y += 48;
                    DrawSignLine(gfx, "Руководитель организации", regularFont, left, right, ref y);
                    y += 16;
                    DrawSignLine(gfx, "С приказом ознакомлен(а)", regularFont, left, right, ref y);

                    y += 18;
                    gfx.DrawString("Дата начала отпуска и его продолжительность подтверждены кадровой службой.", regularFont, XBrushes.Black,
                        new XRect(left, y, width, 20), XStringFormats.TopLeft);

                    y += 22;
                    gfx.DrawString("Документ оформлен с учетом требований ГОСТ Р 7.0.97-2016.",
                        new XFont("Times New Roman", 10, XFontStyle.Italic, unicodeOptions),
                        XBrushes.Gray,
                        new XRect(left, y, width, 20),
                        XStringFormats.TopLeft);
                }

                document.Save(filePath);
            }

            return filePath;
        }

        private static void DrawCentered(XGraphics gfx, string text, XFont font, double x, double y, double width)
        {
            gfx.DrawString(text, font, XBrushes.Black, new XRect(x, y, width, 20), XStringFormats.TopCenter);
        }

        private static void DrawField(XGraphics gfx, string label, string value, XFont font, double left, double right, ref double y)
        {
            var width = right - left;
            gfx.DrawString($"{label}: {value}", font, XBrushes.Black, new XRect(left, y, width, 20), XStringFormats.TopLeft);
            y += 24;
            gfx.DrawLine(XPens.Black, left, y, right, y);
            y += 8;
        }

        private static void DrawSignLine(XGraphics gfx, string label, XFont font, double left, double right, ref double y)
        {
            gfx.DrawString(label, font, XBrushes.Black, new XRect(left, y, 180, 20), XStringFormats.TopLeft);
            gfx.DrawLine(XPens.Black, left + 190, y + 14, right - 120, y + 14);
            gfx.DrawString("(подпись)", new XFont("Times New Roman", 10, XFontStyle.Italic,
                new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always)),
                XBrushes.Gray, new XRect(right - 110, y + 6, 100, 20), XStringFormats.TopLeft);
            y += 24;
        }
    }
}
