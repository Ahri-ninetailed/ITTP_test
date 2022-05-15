using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITTP_test.Models;
using System.Text.RegularExpressions;
namespace ITTP_test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;

        public UsersController(UserContext context)
        {
            _context = context;
        }

        
        //Запрос пользователя по логину и паролю (Доступно только самому пользователю, если он активен(отсутствует RevokedOn))
        // GET: api/Users/Read/ReadByMe
        [HttpGet("Read/ReadByMe")]
        public async Task<ActionResult<IEnumerable<User>>> ReadByMe()
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            if (!IsPasswordTrue(login, password))
                throw new Exception("Неверный логин или пароль");

            //запрос пользователя по логину и паролю, доступно только самому пользователю,. если он активен
            if (IsAdmin(login, password) == false)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
                if (user is null)
                    throw new NullReferenceException("Неверный логин");
                if (user.RevokedOn is not null)
                    throw new Exception("Запись удалена");

                List<User> answer = new List<User>();
                answer.Add(user);

                return answer;
            }

            throw new Exception("Недостаточно прав");
        }

        
        //Запрос всех пользователей старше определённого возраста (Доступно Админам)
        // GET: api/Users/Read/ReadByAge/{age}
        [HttpGet("Read/ReadByAge/{age}")]
        public async Task<ActionResult<IEnumerable<User>>> ReadByAge(int age)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //запрос всех пользователей старше определенного возраста, доступно только админам
            CheckPasswordAndAdminRights(login, password);

            return await _context.Users.Where(u => u.Birthday.HasValue).Where(u => DateTime.Now.Year - u.Birthday.Value.Year > age).ToListAsync();
        }

        
        //Запрос списка всех активных (отсутствует RevokedOn) пользователей, список отсортирован по CreatedOn(Доступно Админам)
        // GET: api/Users/Read/ReadByAllUsers
        [HttpGet("Read/ReadByAllUsers")]
        public async Task<ActionResult<IEnumerable<User>>> ReadByAllUsers()
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //поиск по всем активным записям + сортировка по дате добавления, доступно только админам
            CheckPasswordAndAdminRights(login, password);

            return await _context.Users.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn).ToListAsync();
        }

        
        //Запрос пользователя по логину, в списке долны быть имя, пол и дата рождения статус активный или нет(Доступно Админам)
        // GET: api/Users/Read/ReadByLogin/{findlogin}
        [HttpGet("Read/ReadByLogin/{findlogin}")]
        public async Task<IActionResult> ReadByLogin(string findlogin)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //найти пользователя по логину может 
            CheckPasswordAndAdminRights(login, password);

            User findUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == findlogin);
            if (findUser is null)
                throw new NullReferenceException("Неверный логин");
            return Ok(new { Name = findUser.Name, Gender = findUser.Genger, Birtday = findUser.Birthday, RevokedOn = findUser.RevokedOn });
        }

        
        //Восстановление пользователя - Очистка полей (RevokedOn, RevokedBy) (Доступно Админам)
        // PUT: api/Users/Update-2
        [HttpPut("Update-2/{findlogin}")]
        public async Task<IActionResult> RestoreUser(string findlogin)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //проверим логин/пароль и права для возобновления записи
            CheckPasswordAndAdminRights(login, password);
            User updateUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == findlogin);
            if (updateUser is null)
                throw new NullReferenceException("Не найдена запись с таким логином");
            //очистим поля
            updateUser.RevokedBy = null;
            updateUser.RevokedOn = null;

            updateUser.ModifiedOn = DateTime.Now;
            updateUser.ModifiedBy = login;

            await _context.SaveChangesAsync();
            return CreatedAtAction("ReadByMe", new { Login = login, Password = password }, updateUser);
        }

        
        //Изменение имени, пола или даты рождения пользователя (Может менять Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn))
        // PUT: api/Users/Update-1/UpdateNameGenderBirthday
        [HttpPut("Update-1/UpdateNameGenderBirthday")]
        public async Task<IActionResult> UpdateNameGenderBirthday(NameGenderBirthday nameGenderBirthday)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //запись может менять Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn)
            CheckLoginPasswordAndConditionsToChangeObject(login, password, nameGenderBirthday, out User user);

            user.ModifiedOn = DateTime.Now;
            user.Name = nameGenderBirthday.Name;
            user.Genger = nameGenderBirthday.Genger;
            user.Birthday = nameGenderBirthday.Birthday;

            await _context.SaveChangesAsync();

            return CreatedAtAction("ReadByMe", new { Login = login, Password = password }, user);
        }

        
        //Изменение пароля (Пароль может менять либо Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn))
        // PUT: api/Users/Update-1/UpdatePassword
        [HttpPut("Update-1/UpdatePassword")]
        public async Task<IActionResult> UpdatePassword(NewPassword newPassword)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //запись может менять Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn)
            CheckLoginPasswordAndConditionsToChangeObject(login, password, newPassword, out User user);

            user.ModifiedOn = DateTime.Now;
            user.Password = newPassword.Password;

            await _context.SaveChangesAsync();

            return CreatedAtAction("ReadByMe", new { Login = login, Password = password }, user);
        }

        
        //Изменение логина (Логин может менять либо Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn), логин должен оставаться уникальным)
        // PUT: api/Users/Update-1/UpdateLogin
        [HttpPut("Update-1/UpdateLogin")]
        public async Task<IActionResult> UpdateLogin(NewLoginClass newLoginClass)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //запись может менять Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn)
            CheckLoginPasswordAndConditionsToChangeObject(login, password, newLoginClass, out User user);

            //проверка на уникальность логина
            var checkUser = _context.Users.FirstOrDefault(u => u.Login == newLoginClass.NewLogin);
            if (checkUser != null)
                throw new Exception("Такой логин уже занят");
            
            user.ModifiedOn = DateTime.Now;
            user.Login = newLoginClass.NewLogin;

            await _context.SaveChangesAsync();

            return CreatedAtAction("ReadByMe", new { Login = login, Password = password }, user);
        }

        
        //Создание пользователя по логину, паролю, имени, полу и дате рождения + указание будет ли пользователь админом(Доступно Админам)
        // POST: api/Users/Create
        [HttpPost("Create")]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //добавлять записи может только админ
            CheckPasswordAndAdminRights(login, password);
            user.ModifiedBy = login;
            user.CreatedBy = login;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction("ReadByMe", new { Login = login, Password = password }, user);
        }

        
        //Удаление пользователя по логину полное или мягкое (При мягком удалении должна происходить простановка RevokedOn и RevokedBy) (Доступно Админам)
        // DELETE: api/Users/Delete/{findlogin}/{softorhard}
        [HttpDelete("Delete/{findlogin}/{softorhard}")]
        public async Task<IActionResult> DeleteUser(string findlogin, string softorhard)
        {
            softorhard = softorhard.ToLower();

            //получим логин пароль из хедера
            GetLoginPassword(out string login, out string password);

            //удалять запись может только админ
            CheckPasswordAndAdminRights(login, password);
            //есть два типа удаления: мягкое и нет
            if (!(softorhard == "hard" || softorhard == "soft"))
                throw new Exception("Режим удаления может быть \"soft\" или \"hard\"");

            var user = _context.Users.FirstOrDefault(u => u.Login == findlogin);
            if (user is null)
                throw new Exception("Такой логин не существует");

            if (softorhard == "hard")
                _context.Users.Remove(user);
            else
            {
                var nowTime = DateTime.Now;
                user.RevokedBy = login;
                user.RevokedOn = nowTime;
                user.ModifiedBy = login;
                user.ModifiedOn = nowTime;
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        
        //метод проверяет правильность логина и пароля, и достаточно ли прав для выполнения определенной операции
        private void CheckPasswordAndAdminRights(string login, string password)
        {
            if (!IsPasswordTrue(login, password))
                throw new Exception("Неверный логин или пароль");

            if (IsAdmin(login, password) == false)
                throw new Exception("Недостаточно прав");
        }


        //Метод проверяет подходит ли пароль к логину
        private bool IsPasswordTrue(string login, string password)
        {
            User user = _context.Users.FirstOrDefault(u => u.Login == login);
            if (user is null)
                return false;
            if (user.Password != password)
                return false;
            return true;
        }
        
        
        //Метод преверяет есть ли у аккаунта права
        private bool IsAdmin(string login, string password)
        {
            User user = _context.Users.FirstOrDefault(u => u.Login == login);
            if (user is null)
                return false;
            if (user.Admin)
                return true;
            return false;
        }
        
        
        //метод получает логин и пароль из хедера
        private void GetLoginPassword(out string login, out string password)
        {
            login = HttpContext.Request.Headers["Login"];
            password = HttpContext.Request.Headers["Password"];
        }
        
        
        //запись может менять Администратор, либо лично пользователь, если он активен(отсутствует RevokedOn)
        private void CheckLoginPasswordAndConditionsToChangeObject(string login, string password, FindLoginClass findLoginClass, out User user)
        {
            //проверим логин и пароль
            if (!IsPasswordTrue(login, password))
                throw new Exception("Неверный логин или пароль");

            bool isAdmin = IsAdmin(login, password);

            //если юзер не админ, то он не сможет изменить чужую запись
            if (isAdmin == false && login != findLoginClass.FindLogin)
                throw new Exception("Недостаточно прав");

            //получим объект юзера, которого будем менять
            user = _context.Users.FirstOrDefault(u => u.Login == findLoginClass.FindLogin);

            //пользователь не может изменять свою запись, если он удален
            if (user.RevokedOn is not null && isAdmin == false)
                throw new Exception("Ваша запись была удалена");
        }
    }
    public class FindLoginClass
    {
        //(запрещены все символы кроме латинских букв и цифр)
        private string findLogin;
        public string FindLogin
        {
            get => findLogin;
            set
            {
                if (User.IsLettersAndNumbers(value))
                    findLogin = value;
                else
                    throw new Exception("В логине можно использовать только латинские буквы и цифры");
            }
        }
    }
    public class NameGenderBirthday : FindLoginClass
    {
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
    }
    public class NewPassword : FindLoginClass
    {
        //(запрещены все символы кроме латинских букв и цифр)
        private string password;
        public string Password
        {
            get => password;
            set
            {
                if (User.IsLettersAndNumbers(value))
                    password = value;
                else
                    throw new Exception("Пароль может содержать только латинские буквы и цифры");
            }
        }
    }
    public class NewLoginClass : FindLoginClass
    {
        //(запрещены все символы кроме латинских букв и цифр)
        private string newLogin;
        public string NewLogin
        {
            get => newLogin;
            set
            {
                if (User.IsLettersAndNumbers(value))
                    newLogin = value;
                else
                    throw new Exception("В логине можно использовать только латинские буквы и цифры");
            }
        }
    }
}
