using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using NotesShareApi.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace NotesShareApi.Controllers
{
    [Authorize]
    public class NotesController : ApiController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotesController()
        {
            _dbContext = ApplicationDbContext.Create();
            _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_dbContext));
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetUserNotes(string username)
        {
            var notes = await _dbContext.Notes
                                        .Include(n => n.User)
                                        .Where(n => n.User.UserName == username).ToListAsync();
            return Ok(notes);
        }

        [HttpPost]
        public async Task<IHttpActionResult> AddNote([FromBody]Note note)
        {
            var userName = RequestContext.Principal.Identity.GetUserName();
            var user = await _userManager.FindByNameAsync(userName);
            note.User = user;

            if (ModelState.IsValid)
            {
                _dbContext.Notes.Add(note);
                await _dbContext.SaveChangesAsync();
                return Ok(note);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut]
        public async Task<IHttpActionResult> ModifyNote([FromBody]Note note)
        {
            if (ModelState.IsValid)
            {
                var originalNote = await _dbContext.Notes
                                                    .Include(n => n.User)
                                                    .Where(n => n.Id == note.Id)
                                                    .FirstOrDefaultAsync();
                var user = await _userManager.FindByNameAsync(RequestContext.Principal.Identity.GetUserName());
                if (originalNote.User.UserName != user.UserName)
                    return Unauthorized();

                _dbContext.Entry(originalNote).State = EntityState.Detached;
                note.User = user;
                _dbContext.Entry(note).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();
                return Ok("Note modified");
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteNote([FromUri]int id)
        {
            var note = await _dbContext.Notes
                                       .Include(n => n.User)
                                       .FirstOrDefaultAsync(n => n.Id == id);

            if (note is null)
                return NotFound();
                                       
            if (note.User.UserName != RequestContext.Principal.Identity.GetUserName())
                return Unauthorized();

            _dbContext.Notes.Remove(note);
            await _dbContext.SaveChangesAsync();
            return Ok("Note deleted");
        }
    }
}