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
            VacationStartDatePicker.SelectedDate = DateTime.Today;
            VacationEndDatePicker.SelectedDate = DateTime.Today.AddDays(14);
            SickLeaveStartDatePicker.SelectedDate = DateTime.Today;
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
            SyncCurrentPositionChange(data);

            PositionHistoryList.ItemsSource = data.PositionHistory
                .Where(p => p.EmployeeId == _employee.EmployeeID)
                .OrderBy(p => p.StartDate)
                .ToList();

            VacationList.ItemsSource = data.Vacations
                .Where(v => v.EmployeeId == _employee.EmployeeID)
                .OrderByDescending(v => v.StartDate)
                .ToList();

            var sickLeaves = data.SickLeaves
                .Where(s => s.EmployeeId == _employee.EmployeeID)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SickLeaveViewModel
                {
                    Id = s.Id,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Reason = s.Reason,
                    StatusText = s.EndDate.HasValue ? "Закрыт" : "Открыт"
                })
                .ToList();

            SickLeaveList.ItemsSource = sickLeaves;

            var openSick = data.SickLeaves
                .Where(s => s.EmployeeId == _employee.EmployeeID && !s.EndDate.HasValue)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            SickStatusText.Text = openSick == null
                ? "Статус: нет активного больничного"
                : $"Статус: на больничном с {openSick.StartDate:dd.MM.yyyy}";
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

        private void SyncCurrentPositionChange(HrDataContainer data)
        {
            var openRecord = data.PositionHistory
                .Where(p => p.EmployeeId == _employee.EmployeeID && p.EndDate == null)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefault();

            if (openRecord == null || openRecord.PositionId == _employee.PositionID)
            {
                return;
            }

            var today = DateTime.Today;
            if (openRecord.StartDate.Date <= today)
            {
                openRecord.EndDate = today.AddDays(-1);
            }

            data.PositionHistory.Add(new PositionHistoryRecord
            {
                EmployeeId = _employee.EmployeeID,
                PositionId = _employee.PositionID,
                PositionName = _employee.Positions?.PositionName ?? "Не указано",
                StartDate = today,
                EndDate = null
            });

            HrDataService.Save(data);
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

            if (HrDataService.HasActiveSickLeave(_employee.EmployeeID, startDate) ||
                HrDataService.HasActiveSickLeave(_employee.EmployeeID, endDate))
            {
                MessageBox.Show("Нельзя оформить отпуск, пока сотрудник на больничном", "Внимание");
                return;
            }

            if (HrDataService.HasVacationOverlap(_employee.EmployeeID, startDate, endDate))
            {
                MessageBox.Show("На выбранный период уже есть отпуск", "Внимание");
                return;
            }

            if (HrDataService.WouldLeavePositionWithoutStaff(_employee.PositionID, _employee.EmployeeID, startDate, endDate))
            {
                MessageBox.Show("На этот период по должности не останется сотрудников на смене. Выберите другие даты отпуска.", "Внимание");
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

        private void OpenSickLeave_Click(object sender, RoutedEventArgs e)
        {
            if (!SickLeaveStartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату открытия больничного", "Внимание");
                return;
            }

            var startDate = SickLeaveStartDatePicker.SelectedDate.Value.Date;
            if (startDate < _employee.HireDate.Date)
            {
                MessageBox.Show("Дата больничного не может быть раньше даты приема", "Внимание");
                return;
            }

            var data = HrDataService.Load();
            var openSick = data.SickLeaves
                .FirstOrDefault(s => s.EmployeeId == _employee.EmployeeID && !s.EndDate.HasValue);
            if (openSick != null)
            {
                MessageBox.Show("У сотрудника уже открыт больничный", "Внимание");
                return;
            }

            var hasVacationOverlap = data.Vacations.Any(v =>
                v.EmployeeId == _employee.EmployeeID &&
                startDate >= v.StartDate.Date &&
                startDate <= v.EndDate.Date);
            if (hasVacationOverlap)
            {
                MessageBox.Show("Нельзя открыть больничный в период уже оформленного отпуска", "Внимание");
                return;
            }

            var reason = string.IsNullOrWhiteSpace(SickLeaveReasonTextBox.Text)
                ? "Временная нетрудоспособность"
                : SickLeaveReasonTextBox.Text.Trim();

            data.SickLeaves.Add(new SickLeaveRecord
            {
                EmployeeId = _employee.EmployeeID,
                StartDate = startDate,
                EndDate = null,
                Reason = reason,
                OpenedBy = Environment.UserName
            });

            HrDataService.Save(data);
            MessageBox.Show("Больничный открыт", "Успех");
            RefreshData();
        }

        private void CloseSickLeave_Click(object sender, RoutedEventArgs e)
        {
            var data = HrDataService.Load();
            var openSick = data.SickLeaves
                .Where(s => s.EmployeeId == _employee.EmployeeID && !s.EndDate.HasValue)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (openSick == null)
            {
                MessageBox.Show("У сотрудника нет открытого больничного", "Внимание");
                return;
            }

            openSick.EndDate = DateTime.Today;
            HrDataService.Save(data);
            MessageBox.Show("Больничный закрыт", "Успех");
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

        private sealed class SickLeaveViewModel
        {
            public Guid Id { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Reason { get; set; }
            public string StatusText { get; set; }
        }
    }
}
