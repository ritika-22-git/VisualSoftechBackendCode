using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VisualSoftechBackend.DAO;
using VisualSoftechBackend.Models;

namespace VisualSoftechBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentRepository _repo;
        public StudentsController(IStudentRepository repo) { _repo = repo; }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return NotFound();
            return Ok(s);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StudentMaster model)
        {
            var id = await _repo.CreateAsync(model);
            return CreatedAtAction(nameof(Get), new { id }, model);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StudentMaster model)
        {
            if (id != model.StudentId) return BadRequest("id mismatch");
            await _repo.UpdateAsync(model);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
