using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace vokzal
{
    public partial class EducationPage : Page
    {
        private Employees _currentEmployee;
        private Education _currentEducation;

        public string EmployeeName => _currentEmployee?.FullName ?? "Неизвестно";

        public EducationPage(Employees employee)
        {
            InitializeComponent();
            _currentEmployee = employee;
            DataContext = this;
            LoadEducations();
            ClearForm();
        }

        private void LoadEducations()
        {
            var educations = VokzalEntities.GetContext().Education
                .Where(e => e.EmployeeID == _currentEmployee.EmployeeID)
                .ToList();
            EducationListView.ItemsSource = educations;
        }

        private void AddEducationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                var education = new Education
                {
                    EmployeeID = _currentEmployee.EmployeeID,
                    EducationLevel = (EducationLevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    InstitutionName = InstitutionTBox.Text,
                    GraduationYear = int.Parse(GraduationYearTBox.Text)
                };

                VokzalEntities.GetContext().Education.Add(education);
                VokzalEntities.GetContext().SaveChanges();

                LoadEducations();
                ClearForm();
                MessageBox.Show("Образование добавлено успешно!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка");
            }
        }

        private void UpdateEducationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEducation == null)
            {
                MessageBox.Show("Выберите образование для редактирования", "Внимание");
                return;
            }

            if (!ValidateForm()) return;

            try
            {
                _currentEducation.EducationLevel = (EducationLevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                _currentEducation.InstitutionName = InstitutionTBox.Text;
                _currentEducation.GraduationYear = int.Parse(GraduationYearTBox.Text);

                VokzalEntities.GetContext().SaveChanges();

                LoadEducations();
                ClearForm();
                MessageBox.Show("Образование обновлено успешно!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка");
            }
        }

        private void DeleteEducationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEducation == null)
            {
                MessageBox.Show("Выберите образование для удаления", "Внимание");
                return;
            }

            if (MessageBox.Show("Удалить выбранное образование?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    VokzalEntities.GetContext().Education.Remove(_currentEducation);
                    VokzalEntities.GetContext().SaveChanges();

                    LoadEducations();
                    ClearForm();
                    MessageBox.Show("Образование удалено успешно!", "Успех");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка");
                }
            }
        }

        private void ClearFormBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void EducationListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentEducation = EducationListView.SelectedItem as Education;
            if (_currentEducation != null)
            {
                // Заполняем форму данными выбранного образования
                foreach (ComboBoxItem item in EducationLevelComboBox.Items)
                {
                    if (item.Content.ToString() == _currentEducation.EducationLevel)
                    {
                        EducationLevelComboBox.SelectedItem = item;
                        break;
                    }
                }
                InstitutionTBox.Text = _currentEducation.InstitutionName;
                GraduationYearTBox.Text = _currentEducation.GraduationYear.ToString();
            }
        }

        private bool ValidateForm()
        {
            if (EducationLevelComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите уровень образования", "Ошибка");
                return false;
            }

            if (string.IsNullOrWhiteSpace(InstitutionTBox.Text))
            {
                MessageBox.Show("Введите название учебного заведения", "Ошибка");
                return false;
            }

            if (!int.TryParse(GraduationYearTBox.Text, out int year) || year < 1900 || year > DateTime.Now.Year)
            {
                MessageBox.Show("Введите корректный год окончания (от 1900 до текущего года)", "Ошибка");
                return false;
            }

            string educationLevel = (EducationLevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            int graduationYear = int.Parse(GraduationYearTBox.Text);

            // Проверка что сотрудник не закончил вуз до рождения
            if (graduationYear < _currentEmployee.BirthDate.Year)
            {
                MessageBox.Show("Год окончания не может быть раньше года рождения", "Ошибка");
                return false;
            }

            int employeeAgeAtGraduation = graduationYear - _currentEmployee.BirthDate.Year;

            // Валидация по возрасту для всех типов образования
            switch (educationLevel)
            {
                case "Высшее":
                    if (employeeAgeAtGraduation < 17)
                    {
                        MessageBox.Show("Слишком ранний возраст для получения высшего образования (минимум 17 лет)", "Ошибка");
                        return false;
                    }
                    if (employeeAgeAtGraduation > 70)
                    {
                        MessageBox.Show("Слишком поздний возраст для получения высшего образования", "Ошибка");
                        return false;
                    }
                    break;

                case "Среднее специальное":
                    if (employeeAgeAtGraduation < 16)
                    {
                        MessageBox.Show("Слишком ранний возраст для получения среднего специального образования (минимум 16 лет)", "Ошибка");
                        return false;
                    }
                    if (employeeAgeAtGraduation > 65)
                    {
                        MessageBox.Show("Слишком поздний возраст для получения среднего специального образования", "Ошибка");
                        return false;
                    }
                    break;

                case "Среднее":
                    if (employeeAgeAtGraduation < 14)
                    {
                        MessageBox.Show("Слишком ранний возраст для получения среднего образования (минимум 14 лет)", "Ошибка");
                        return false;
                    }
                    if (employeeAgeAtGraduation > 25)
                    {
                        MessageBox.Show("Слишком поздний возраст для получения среднего образования", "Ошибка");
                        return false;
                    }
                    break;

                case "Неоконченное высшее":
                    if (employeeAgeAtGraduation < 16)
                    {
                        MessageBox.Show("Слишком ранний возраст для неоконченного высшего образования (минимум 16 лет)", "Ошибка");
                        return false;
                    }
                    break;

                case "Аспирантура":
                    if (employeeAgeAtGraduation < 22)
                    {
                        MessageBox.Show("Слишком ранний возраст для аспирантуры (минимум 22 года)", "Ошибка");
                        return false;
                    }
                    break;

                case "Докторантура":
                    if (employeeAgeAtGraduation < 25)
                    {
                        MessageBox.Show("Слишком ранний возраст для докторантуры (минимум 25 лет)", "Ошибка");
                        return false;
                    }
                    break;
            }

            return true;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }

        private void ClearForm()
        {
            _currentEducation = null;
            EducationLevelComboBox.SelectedIndex = -1;
            InstitutionTBox.Clear();
            GraduationYearTBox.Clear();
            EducationListView.SelectedItem = null;
        }
    }
}