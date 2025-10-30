using VisualSoftechBackend.Models;

namespace VisualSoftechBackend.DAO
{
    public interface IStudentRepository
    {
        Task<IEnumerable<StudentMaster>> GetAllAsync();
        Task<StudentMaster?> GetByIdAsync(int id);
        Task<int> CreateAsync(StudentMaster s);
        Task UpdateAsync(StudentMaster s);
        Task DeleteAsync(int id);
    }
}
