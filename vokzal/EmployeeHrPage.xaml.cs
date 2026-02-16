using System;
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
            PositionStartDatePicker.SelectedDate = DateTime.Today;
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
            PositionHistoryList.ItemsSource = data.PositionHistory
                .Where(p => p.EmployeeId == _employee.EmployeeID)
                .OrderByDescending(p => p.StartDate)
                .ToList();

            VacationList.ItemsSource = data.Vacations
                .Where(v => v.EmployeeId == _employee.EmployeeID)
                .OrderByDescending(v => v.StartDate)
                .ToList();
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
                MessageBox.Show($"Отпуск добавлен. PDF приказ создан:\n{pdfPath}", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Отпуск сохранен, но PDF не удалось создать: {ex.Message}\n\nПроверьте восстановление NuGet-пакетов (iTextSharp и Portable.BouncyCastle).", "Предупреждение");
            }

            RefreshData();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}
