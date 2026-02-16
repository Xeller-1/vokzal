using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class EmployeeDocumentsPage : Page
    {
        private Employees _currentEmployee;
        private EmployeeDocuments _currentDocument;

        public string EmployeeName => _currentEmployee?.FullName ?? "Неизвестно";
        public EmployeeDocuments CurrentDocument => _currentDocument;

        public EmployeeDocumentsPage(Employees employee)
        {
            InitializeComponent();
            _currentEmployee = employee;
            LoadDocument();
            DataContext = this;
        }
        private EmployeeDocuments GetEmployeeDocuments(int employeeId)
        {
            using (var context = VokzalEntities.GetContext())
            {
                return context.EmployeeDocuments.FirstOrDefault(ed => ed.EmployeeID == employeeId);
            }
        }
        private void LoadDocument()
        {
            var context = VokzalEntities.GetContext();
            _currentDocument = context.EmployeeDocuments.FirstOrDefault(ed => ed.EmployeeID == _currentEmployee.EmployeeID);

            if (_currentDocument == null)
            {
                _currentDocument = new EmployeeDocuments
                {
                    EmployeeID = _currentEmployee.EmployeeID,
                    PassportData = "",
                    PassportIssuedBy = "",
                    PassportIssueDate = DateTime.Now,
                    INN = "",
                    SNILS = "",
                    MedicalPolicy = "",
                    CreatedDate = DateTime.Now
                };
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            // Валидация паспорта
            if (string.IsNullOrWhiteSpace(_currentDocument.PassportData))
                errors.AppendLine("Укажите паспортные данные");
            else if (!Regex.IsMatch(_currentDocument.PassportData, @"^\d{4} \d{6}$"))
                errors.AppendLine("Неверный формат паспорта. Должен быть: XXXX XXXXXX");

            // Проверка возраста для паспорта (не моложе 14 лет)
            if (_currentDocument.PassportIssueDate != null && _currentEmployee.BirthDate != null)
            {
                DateTime passportIssueDate = _currentDocument.PassportIssueDate;
                DateTime birthDate = _currentEmployee.BirthDate;

                if (passportIssueDate < birthDate.AddYears(14))
                    errors.AppendLine("Паспорт не может быть выдан лицу младше 14 лет");
            }

            // Валидация ИНН
            if (string.IsNullOrWhiteSpace(_currentDocument.INN))
                errors.AppendLine("Укажите ИНН");
            else if (!Regex.IsMatch(_currentDocument.INN, @"^\d{12}$"))
                errors.AppendLine("ИНН должен содержать 12 цифр");

            // Валидация СНИЛС
            if (string.IsNullOrWhiteSpace(_currentDocument.SNILS))
                errors.AppendLine("Укажите СНИЛС");
            else if (!Regex.IsMatch(_currentDocument.SNILS, @"^\d{11}$"))
                errors.AppendLine("СНИЛС должен содержать 11 цифр");

            // Валидация мед полиса
            if (string.IsNullOrWhiteSpace(_currentDocument.MedicalPolicy))
                errors.AppendLine("Укажите медицинский полис");
            else if (!Regex.IsMatch(_currentDocument.MedicalPolicy, @"^\d{16}$"))
                errors.AppendLine("Медицинский полис должен содержать 16 цифр");

            if (string.IsNullOrWhiteSpace(_currentDocument.PassportIssuedBy))
                errors.AppendLine("Укажите кем выдан паспорт");

            if (_currentDocument.PassportIssueDate > DateTime.Now)
                errors.AppendLine("Дата выдачи паспорта не может быть в будущем");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации");
                return;
            }

            try
            {

                if (_currentDocument.DocumentID == 0)
                {
                    VokzalEntities.GetContext().EmployeeDocuments.Add(_currentDocument);
                }

                VokzalEntities.GetContext().SaveChanges();
                MessageBox.Show("Данные сохранены успешно!", "Успех");
                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}