using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace vokzal
{
    public partial class AddEditPage : Page
    {
        private Employees currentPeople = new Employees();

        public AddEditPage(Employees selectedPeople)
        {
            InitializeComponent();

            // Загрузка должностей в ComboBox
            PositionComboBox.ItemsSource = VokzalEntities.GetContext().Positions.ToList();
            PositionComboBox.DisplayMemberPath = "PositionName";
            PositionComboBox.SelectedValuePath = "PositionID";

            if (selectedPeople != null)
            {
                currentPeople = selectedPeople;
                EditTitle.Visibility = Visibility.Visible;
                AddTitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                EditTitle.Visibility = Visibility.Collapsed;
                AddTitle.Visibility = Visibility.Visible;
            }

            DataContext = currentPeople;

            if (currentPeople.Gender == "Ж")
            {
                FemaleRBtn.IsChecked = true;
            }
            else
            {
                MaleRBtn.IsChecked = true;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();
            var oldPositionId = currentPeople.EmployeeID == 0
                ? (int?)null
                : VokzalEntities.GetContext().Employees
                    .AsNoTracking()
                    .Where(x => x.EmployeeID == currentPeople.EmployeeID)
                    .Select(x => (int?)x.PositionID)
                    .FirstOrDefault();

            // Проверки ФИО
            if (string.IsNullOrWhiteSpace(currentPeople.LastName))
                errors.AppendLine("Укажите фамилию сотрудника");
            else if (currentPeople.LastName.Length > 50)
                errors.AppendLine("Превышен лимит символов фамилии");
            else if (!Regex.IsMatch(currentPeople.LastName, @"^[a-zA-Zа-яА-ЯёЁ\s-]+$"))
                errors.AppendLine("Фамилия может содержать только буквы, пробелы и дефисы");

            if (string.IsNullOrWhiteSpace(currentPeople.FirstName))
                errors.AppendLine("Укажите имя сотрудника");
            else if (currentPeople.FirstName.Length > 50)
                errors.AppendLine("Превышен лимит символов имени");
            else if (!Regex.IsMatch(currentPeople.FirstName, @"^[a-zA-Zа-яА-ЯёЁ\s-]+$"))
                errors.AppendLine("Имя может содержать только буквы, пробелы и дефисы");

            if (string.IsNullOrWhiteSpace(currentPeople.MiddleName))
                errors.AppendLine("Укажите отчество сотрудника");
            else if (currentPeople.MiddleName.Length > 50)
                errors.AppendLine("Превышен лимит символов отчества");
            else if (!Regex.IsMatch(currentPeople.MiddleName, @"^[a-zA-Zа-яА-ЯёЁ\s-]+$"))
                errors.AppendLine("Отчество может содержать только буквы, пробелы и дефисы");

            // Проверка даты рождения
            if (BirthDatePicker.SelectedDate == null)
            {
                errors.AppendLine("Укажите дату рождения сотрудника");
            }
            else
            {
                DateTime birthDate = BirthDatePicker.SelectedDate.Value;
                if (birthDate > DateTime.Now)
                    errors.AppendLine("Дата рождения не может быть в будущем");
                else if (birthDate < DateTime.Now.AddYears(-100))
                    errors.AppendLine("Проверьте дату рождения - сотрудник слишком стар");
                else
                    currentPeople.BirthDate = birthDate;
            }

            // Проверка даты поступления на работу
            if (HireDatePicker.SelectedDate == null)
            {
                errors.AppendLine("Укажите дату поступления на работу");
            }
            else
            {
                DateTime hireDate = HireDatePicker.SelectedDate.Value;
                if (hireDate > DateTime.Now)
                    errors.AppendLine("Дата поступления не может быть в будущем");

                // Проверка что сотруднику не менее 18 лет на момент поступления
                if (hireDate < currentPeople.BirthDate.AddYears(18))
                    errors.AppendLine("Сотрудник не может быть принят на работу, если ему меньше 18 лет");
                else
                    currentPeople.HireDate = hireDate;
            }

            // Проверка адреса
            if (string.IsNullOrWhiteSpace(currentPeople.Address))
            {
                errors.AppendLine("Укажите адрес проживания");
            }

            // Проверка адреса регистрации
            if (string.IsNullOrWhiteSpace(currentPeople.Registration))
            {
                errors.AppendLine("Укажите адрес регистрации");
            }

            // Проверка телефона
            if (string.IsNullOrWhiteSpace(currentPeople.Phone))
            {
                errors.AppendLine("Укажите номер телефона");
            }
            else
            {
                // Очищаем номер и сохраняем очищенную версию
                string cleanedNumber = Regex.Replace(currentPeople.Phone, @"[^\d+]", "");
                currentPeople.Phone = cleanedNumber; // Сохраняем очищенный номер

                if (!Regex.IsMatch(cleanedNumber, @"^(\+7|7|8)?[\d]{10}$"))
                {
                    errors.AppendLine("Укажите корректный телефон сотрудника");
                }
            }

            // Проверка должности
            if (PositionComboBox.SelectedItem == null)
            {
                errors.AppendLine("Выберите должность сотрудника");
            }
            else
            {
                currentPeople.PositionID = (int)PositionComboBox.SelectedValue;
            }

            if (FemaleRBtn.IsChecked == true)
            {
                currentPeople.Gender = "Ж";
            }
            else
            {
                currentPeople.Gender = "М";
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            if (currentPeople.EmployeeID == 0)
            {
                VokzalEntities.GetContext().Employees.Add(currentPeople);
            }

            try
            {
                VokzalEntities.GetContext().SaveChanges();
                SyncPositionHistory(oldPositionId, currentPeople);
                MessageBox.Show("Информация сохранена");

                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void SyncPositionHistory(int? oldPositionId, Employees employee)
        {
            if (employee == null)
            {
                return;
            }

            var data = HrDataService.Load();
            var employeeHistory = data.PositionHistory
                .Where(p => p.EmployeeId == employee.EmployeeID)
                .OrderBy(p => p.StartDate)
                .ToList();

            if (!employeeHistory.Any())
            {
                var position = VokzalEntities.GetContext().Positions.FirstOrDefault(p => p.PositionID == employee.PositionID);
                data.PositionHistory.Add(new PositionHistoryRecord
                {
                    EmployeeId = employee.EmployeeID,
                    PositionId = employee.PositionID,
                    PositionName = position?.PositionName ?? "Не указано",
                    StartDate = employee.HireDate.Date,
                    EndDate = null
                });
                HrDataService.Save(data);
                return;
            }

            if (oldPositionId.HasValue && oldPositionId.Value == employee.PositionID)
            {
                return;
            }

            var openRecord = employeeHistory.LastOrDefault(p => p.EndDate == null);
            if (openRecord != null)
            {
                openRecord.EndDate = DateTime.Today.AddDays(-1);
            }

            var currentPosition = VokzalEntities.GetContext().Positions.FirstOrDefault(p => p.PositionID == employee.PositionID);
            data.PositionHistory.Add(new PositionHistoryRecord
            {
                EmployeeId = employee.EmployeeID,
                PositionId = employee.PositionID,
                PositionName = currentPosition?.PositionName ?? "Не указано",
                StartDate = DateTime.Today,
                EndDate = null
            });

            HrDataService.Save(data);
        }

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myOpenFileDialog = new OpenFileDialog();
            if (myOpenFileDialog.ShowDialog() == true)
            {
                currentPeople.Photo = myOpenFileDialog.FileName;
                PhotoPeople.Source = new BitmapImage(new Uri(myOpenFileDialog.FileName));
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}