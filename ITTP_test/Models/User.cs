using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace ITTP_test.Models
{
    public class User
    {
        [Key]
        public Guid Guid { get; set; } = Guid.NewGuid();

        //(запрещены все символы кроме латинских букв и цифр)
        private string login;
        public string Login
        {
            get => login;
            set
            {
                if (IsLettersAndNumbers(value))
                    login = value;
                else
                    throw new Exception("В логине можно использовать только латинские буквы и цифры");
            }
        }

        //(запрещены все символы кроме латинских букв и цифр)
        private string password;
        public string Password
        {
            get => password;
            set
            {
                if (IsLettersAndNumbers(value))
                    password = value;
                else
                    throw new Exception("Пароль может содержать только латинские буквы и цифры");
            }
        }

        //(запрещены все символы кроме латинских и русских букв)
        private string name;
        public string Name 
        {
            get => name;
            set
            {
                if (Regex.IsMatch(value, @"^[a-zA-Zа-яА-Я]+$"))
                    name = value;
                else
                    throw new Exception("Имя может содержать только латинские и русские буквы");
            }
        }

        //0 женщина, 1 мужчина, 2 неизвестно
        enum Genders { female, male, unknown}
        private int gender;
        public int Genger
        {
            get => gender;
            set
            {
                if (value == (int)Genders.female || value == (int)Genders.male || value == (int)Genders.unknown)
                    gender = value;
                else
                    throw new Exception("Некорректный пол");
            }
        }

        public DateTime? Birthday { get; set; }

        public bool Admin { get; set; }

        public DateTime CreatedOn { get; set; }

        public string CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime RevokedOn { get; set; }

        public string RevokedBy { get; set; }

        //метод проверяет строку на лат. буквы и цифры, если в строке есть другие символы метод вернет False
        private static bool IsLettersAndNumbers(string value)
        {
            return Regex.IsMatch(value, @"^[0-9a-zA-Z]+$");
        }
    }
}
