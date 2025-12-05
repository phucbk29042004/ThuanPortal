using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly AppBookStoreContext _context;

        public AdminCategoriesController(AppBookStoreContext context)
        {
            _context = context;
        }

        // GET: api/admin/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/admin/categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            return Ok(new
            {
                category.CategoryId,
                category.CategoryName
            });
        }

        // POST: api/admin/categories
        [HttpPost]
        public async Task<ActionResult<object>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var category = new Category
            {
                CategoryName = request.CategoryName
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, new
            {
                message = "Category created successfully",
                categoryId = category.CategoryId
            });
        }

        // PUT: api/admin/categories/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            category.CategoryName = request.CategoryName ?? category.CategoryName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully" });
        }

        // DELETE: api/admin/categories/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully" });
        }
    }

    public class CreateCategoryRequest
    {
        public string? CategoryName { get; set; }
    }

    public class UpdateCategoryRequest
    {
        public string? CategoryName { get; set; }
    }
}

