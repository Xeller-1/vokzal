using System;

namespace vokzal
{
    public class EmployeeValidator
    {
        public bool ValidateBirthDate(DateTime? birthDate, DateTime? hireDate)
        {
            // Проверка на null
            if (birthDate == null || hireDate == null)
                return false;

            DateTime birth = birthDate.Value;
            DateTime hire = hireDate.Value;

            // Дата рождения не может быть в будущем
            if (birth > DateTime.Now)
                return false;

            // Проверка на слишком старый возраст (150 лет)
            if (birth < DateTime.Now.AddYears(-150))
                return false;

            // Сотрудник должен быть не младше 18 лет на момент трудоустройства
            if (hire < birth.AddYears(18))
                return false;

            return true;
        }
    }
}