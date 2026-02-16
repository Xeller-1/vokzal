using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using vokzal;

namespace vokzalTests
{
    [TestClass]
    public class EmployeeValidatorTests
    {
        private EmployeeValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new EmployeeValidator();
        }

        [TestMethod]
        public void ValidateBirthDate_25YearsOldHiredAt20_ReturnsTrue()
        {
            // Проверка стандартного валидного случая: сотрудник родился в 1990, 
            // начал работать в 2010 (в 20 лет) - все условия соблюдены
            DateTime birthDate = new DateTime(1990, 5, 15);
            DateTime hireDate = new DateTime(2010, 6, 20);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateBirthDate_Exactly18YearsOldHired_ReturnsTrue()
        {
            // Проверка пограничного случая: сотруднику ровно 18 лет 
            // на момент трудоустройства - минимальный допустимый возраст
            DateTime birthDate = new DateTime(2000, 1, 1);
            DateTime hireDate = new DateTime(2018, 1, 1);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateBirthDate_15YearsOldHired_ReturnsFalse()
        {
            // Проверка невалидного случая: сотрудник младше 18 лет 
            // на момент трудоустройства (15 лет) - должно вернуть false
            DateTime birthDate = new DateTime(2005, 5, 15);
            DateTime hireDate = new DateTime(2020, 6, 20);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_17Years364DaysHired_ReturnsFalse()
        {
            // Проверка пограничного невалидного случая: сотруднику не хватает 
            // одного дня до 18 лет на момент трудоустройства - должно вернуть false
            DateTime birthDate = new DateTime(2000, 1, 1);
            DateTime hireDate = new DateTime(2017, 12, 31);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_FutureBirthDateHiredIn19Years_ReturnsFalse()
        {
            // Проверка невалидного случая: дата рождения в будущем - 
            // сотрудник еще не родился, но уже трудоустроен - должно вернуть false
            DateTime birthDate = new DateTime(2030, 1, 1); // Будущая дата
            DateTime hireDate = new DateTime(2048, 1, 1);  // Через 18 лет

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_151YearsOldHiredAt21_ReturnsFalse()
        {
            // Проверка невалидного случая: сотрудник слишком стар (151 год) - 
            // превышает максимальный допустимый возраст 150 лет - должно вернуть false
            DateTime birthDate = new DateTime(1874, 1, 1);  // 151 год назад от 2025
            DateTime hireDate = new DateTime(1895, 1, 1);   // Устроился в 21 год

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_150YearsOldHiredAt20_ReturnsTrue()
        {
            // Проверка границы 150 лет от текущей даты (2025)
            DateTime birthDate = new DateTime(1875, 10, 29);  // Ровно 150 лет назад от 29.10.2025
            DateTime hireDate = new DateTime(1895, 10, 29);   // Устроился в 20 лет

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateBirthDate_BothDatesNull_ReturnsFalse()
        {
            // Проверка обработки null значений: обе даты не указаны - 
            // система должна корректно обработать и вернуть false
            DateTime? birthDate = null;
            DateTime? hireDate = null;

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_BirthDateNullHireDate2010_ReturnsFalse()
        {
            // Проверка обработки null значений: дата рождения не указана, 
            // но дата трудоустройства указана - должно вернуть false
            DateTime? birthDate = null;
            DateTime? hireDate = new DateTime(2010, 6, 20);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_HireDateNullBirthDate1990_ReturnsFalse()
        {
            // Проверка обработки null значений: дата трудоустройства не указана, 
            // но дата рождения указана - должно вернуть false
            DateTime? birthDate = new DateTime(1990, 5, 15);
            DateTime? hireDate = null;

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_HireDateBeforeBirthDate_ReturnsFalse()
        {
            // Проверка логической ошибки: дата трудоустройства раньше даты рождения - 
            // сотрудник начал работать до того как родился - должно вернуть false
            DateTime birthDate = new DateTime(1990, 1, 1);
            DateTime hireDate = new DateTime(1980, 1, 1);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_HiredOn18thBirthday_ReturnsTrue()
        {
            // Проверка точного расчета 18 лет: сотрудник трудоустроен 
            // в день своего 18-летия - должно вернуть true
            DateTime birthDate = new DateTime(2000, 6, 1);
            DateTime hireDate = new DateTime(2018, 6, 1);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateBirthDate_HiredOneDayBefore18thBirthday_ReturnsFalse()
        {
            // Проверка точного расчета 18 лет: сотрудник трудоустроен 
            // за день до 18-летия - должно вернуть false
            DateTime birthDate = new DateTime(2000, 6, 1);
            DateTime hireDate = new DateTime(2018, 5, 31);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateBirthDate_100YearsOldHiredAt30_ReturnsTrue()
        {
            // Проверка валидного случая с пожилым сотрудником: 
            // сотрудник родился в 1920, начал работать в 1950 (в 30 лет) - 
            // возраст в пределах 150 лет, должно вернуть true
            DateTime birthDate = new DateTime(1920, 1, 1);
            DateTime hireDate = new DateTime(1950, 1, 1);

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateBirthDate_BornTodayHiredToday_ReturnsFalse()
        {
            // Проверка: сотрудник родился СЕГОДНЯ и устраивается СЕГОДНЯ
            // Ему 0 лет - это невалидно
            DateTime birthDate = new DateTime(2025, 10, 29); // Сегодня
            DateTime hireDate = new DateTime(2025, 10, 29);  // Сегодня

            bool result = _validator.ValidateBirthDate(birthDate, hireDate);

            Assert.IsFalse(result);
        }
    }
}