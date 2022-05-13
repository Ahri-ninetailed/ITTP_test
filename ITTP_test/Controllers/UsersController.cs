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

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
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

        // POST: api/Users/Create
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Create")]
        public async Task<ActionResult<User>> PostUser(User user, string login, string password)
        {
            //добавлять записи может только админ
            CheckPasswordAndRight(login, password);
            user.ModifiedBy = login;
            user.CreatedBy = login;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Guid }, user);
        }

        // DELETE: api/Users/Delete
        [HttpDelete("Delete/{findlogin}/{softorhard}")]
        public async Task<IActionResult> DeleteUser(string findlogin, string softorhard, string login, string password)
        {
            //удалять запись может только админ
            CheckPasswordAndRight(login, password);
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
                user.RevokedBy = login;
                user.RevokedOn = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //метод проверяет правильность логина и пароля, и достаточно ли прав для выполнения определенной операции
        private void CheckPasswordAndRight(string login, string password)
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
    }
}
