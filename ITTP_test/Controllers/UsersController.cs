using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITTP_test.Models;

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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(Guid id, User user)
        {
            if (id != user.Guid)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        //Создание пользователя по логину, паролю, имени, полу и дате рождения + указание будет ли пользователь админом(Доступно Админам)
        // POST: api/Users/Create
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.Guid == id);
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
    }
}
