using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class EmployeeHrPage : Page
    {
        private readonly Employees _employee;

        public EmployeeHrPage(Employees employee)
        {
            InitializeComponent();
            _employee = employee;
            PositionCombo.ItemsSource = VokzalEntities.GetContext().Positions.ToList();
            PositionStartDatePicker.SelectedDate = _employee.HireDate.Date;
            VacationStartDatePicker.SelectedDate = DateTime.Today;
            VacationEndDatePicker.SelectedDate = DateTime.Today.AddDays(14);
            BindHeader();
            RefreshData();
        }

        private void BindHeader()
        {
            EmployeeNameText.Text = _employee.FullName;
            HireDateText.Text = $"Принят на работу: {_employee.HireDate:dd.MM.yyyy}";
            CurrentPositionText.Text = $"Текущая должность: {_employee.Positions?.PositionName}";
        }

        private void RefreshData()
        {
            var data = HrDataService.Load();
            EnsureInitialPositionHistory(data);

            PositionHistoryList.ItemsSource = data.PositionHistory
                .Where(p => p.EmployeeId == _employee.EmployeeID)
                .OrderBy(p => p.StartDate)
                .ToList();

            VacationList.ItemsSource = data.Vacations
                .Where(v => v.EmployeeId == _employee.EmployeeID)
                .OrderByDescending(v => v.StartDate)
                .ToList();
        }

        private void EnsureInitialPositionHistory(HrDataContainer data)
        {
            var hasRecords = data.PositionHistory.Any(p => p.EmployeeId == _employee.EmployeeID);
            if (hasRecords)
            {
                return;
            }

            var currentPosition = VokzalEntities.GetContext().Positions
                .FirstOrDefault(p => p.PositionID == _employee.PositionID);

            data.PositionHistory.Add(new PositionHistoryRecord
            {
                EmployeeId = _employee.EmployeeID,
                PositionId = _employee.PositionID,
                PositionName = currentPosition?.PositionName ?? _employee.Positions?.PositionName ?? "Не указано",
                StartDate = _employee.HireDate.Date,
                EndDate = null
            });

            HrDataService.Save(data);
        }

        private void AddPositionHistory_Click(object sender, RoutedEventArgs e)
        {
            if (PositionCombo.SelectedItem == null || PositionStartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите должность и дату начала", "Внимание");
                return;
            }

            var selectedPosition = (Positions)PositionCombo.SelectedItem;
            var startDate = PositionStartDatePicker.SelectedDate.Value;
            var data = HrDataService.Load();

            var openRecord = data.PositionHistory
                .Where(p => p.EmployeeId == _employee.EmployeeID && p.EndDate == null)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefault();

            if (openRecord != null)
            {
                if (startDate <= openRecord.StartDate)
                {
                    MessageBox.Show("Дата новой должности должна быть позже предыдущей", "Внимание");
                    return;
                }

                openRecord.EndDate = startDate.AddDays(-1);
            }

            data.PositionHistory.Add(new PositionHistoryRecord
            {
                EmployeeId = _employee.EmployeeID,
                PositionId = selectedPosition.PositionID,
                PositionName = selectedPosition.PositionName,
                StartDate = startDate
            });

            HrDataService.Save(data);
            MessageBox.Show("История должностей обновлена", "Успех");
            RefreshData();
        }

        private void AddVacation_Click(object sender, RoutedEventArgs e)
        {
            if (VacationStartDatePicker.SelectedDate == null || VacationEndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите период отпуска", "Внимание");
                return;
            }

            var startDate = VacationStartDatePicker.SelectedDate.Value.Date;
            var endDate = VacationEndDatePicker.SelectedDate.Value.Date;
            if (endDate < startDate)
            {
                MessageBox.Show("Дата окончания отпуска раньше даты начала", "Внимание");
                return;
            }

            if (HrDataService.HasVacationOverlap(_employee.EmployeeID, startDate, endDate))
            {
                MessageBox.Show("На выбранный период уже есть отпуск", "Внимание");
                return;
            }

            var reason = string.IsNullOrWhiteSpace(VacationReasonTextBox.Text)
                ? "Ежегодный оплачиваемый отпуск"
                : VacationReasonTextBox.Text.Trim();

            var data = HrDataService.Load();
            var booking = new VacationBooking
            {
                EmployeeId = _employee.EmployeeID,
                StartDate = startDate,
                EndDate = endDate,
                Reason = reason
            };

            data.Vacations.Add(booking);
            HrDataService.Save(data);

            try
            {
                var pdfPath = PdfVacationOrderGenerator.Generate(_employee, booking);
                booking.PdfPath = pdfPath;
                HrDataService.Save(data);
                MessageBox.Show($"Отпуск добавлен. PDF приказ создан:\n{pdfPath}", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Отпуск сохранен, но PDF не удалось создать: {ex.Message}\n\nПроверьте восстановление NuGet-пакетов (iTextSharp и Portable.BouncyCastle).", "Предупреждение");
            }

            RefreshData();
        }


        private void VacationList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var vacation = VacationList.SelectedItem as VacationBooking;
            if (vacation == null)
            {
                return;
            }

            var pdfPath = ResolveVacationPdfPath(vacation);
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                MessageBox.Show("Файл PDF не найден для выбранного отпуска", "Внимание");
                return;
            }

            if (vacation.PdfPath != pdfPath)
            {
                vacation.PdfPath = pdfPath;
                var data = HrDataService.Load();
                var stored = data.Vacations.FirstOrDefault(v => v.Id == vacation.Id);
                if (stored != null)
                {
                    stored.PdfPath = pdfPath;
                    HrDataService.Save(data);
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть PDF: {ex.Message}", "Ошибка");
            }
        }

        private string ResolveVacationPdfPath(VacationBooking vacation)
        {
            if (!string.IsNullOrWhiteSpace(vacation.PdfPath) && System.IO.File.Exists(vacation.PdfPath))
            {
                return vacation.PdfPath;
            }

            var fileName = $"Prikaz_otpuska_{_employee.EmployeeID}_{vacation.StartDate:yyyyMMdd}.pdf";
            var currentDirPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orders", fileName);
            if (System.IO.File.Exists(currentDirPath))
            {
                return currentDirPath;
            }

            var localAppDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "vokzal",
                "Orders",
                fileName);

            return System.IO.File.Exists(localAppDataPath) ? localAppDataPath : null;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}
