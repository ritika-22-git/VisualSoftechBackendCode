using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VisualSoftechBackend.Models;

namespace VisualSoftechBackend.DAO
{
    public class StudentRepository : IStudentRepository
    {
        private readonly string _conn;
        public StudentRepository(IConfiguration config) => _conn = config.GetConnectionString("DefaultConnection");

        public async Task<IEnumerable<StudentMaster>> GetAllAsync()
        {
            var list = new List<StudentMaster>();
            const string sql = "SELECT StudentId, Name, Age, Address, StateId, PhoneNumber, PhotoPath FROM dbo.Student_Master ORDER BY StudentId";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new StudentMaster
                {
                    StudentId = rdr.GetInt32(0),
                    Name = rdr.GetString(1),
                    Age = rdr.GetInt32(2),
                    Address = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    StateId = rdr.GetInt32(4),
                    PhoneNumber = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    PhotoPath = rdr.IsDBNull(6) ? null : rdr.GetString(6)
                });
            }
            // Optionally fetch subjects per student (simple approach)
            foreach (var s in list)
                s.Subjects = (await GetSubjectsForStudentAsync(s.StudentId)).ToList();
            return list;
        }

        public async Task<StudentMaster?> GetByIdAsync(int id)
        {
            const string sql = "SELECT StudentId, Name, Age, Address, StateId, PhoneNumber, PhotoPath FROM dbo.Student_Master WHERE StudentId = @Id";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await rdr.ReadAsync()) return null;
            var s = new StudentMaster
            {
                StudentId = rdr.GetInt32(0),
                Name = rdr.GetString(1),
                Age = rdr.GetInt32(2),
                Address = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                StateId = rdr.GetInt32(4),
                PhoneNumber = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                PhotoPath = rdr.IsDBNull(6) ? null : rdr.GetString(6)
            };
            s.Subjects = (await GetSubjectsForStudentAsync(s.StudentId)).ToList();
            return s;
        }

        public async Task<int> CreateAsync(StudentMaster s)
        {
            const string sql = @"INSERT INTO dbo.Student_Master (Name, Age, Address, StateId, PhoneNumber, PhotoPath)
                             VALUES (@Name,@Age,@Address,@StateId,@PhoneNumber,@PhotoPath);
                             SELECT CAST(SCOPE_IDENTITY() as int);";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Name", s.Name);
            cmd.Parameters.AddWithValue("@Age", s.Age);
            cmd.Parameters.AddWithValue("@Address", (object?)s.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StateId", s.StateId);
            cmd.Parameters.AddWithValue("@PhoneNumber", (object?)s.PhoneNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PhotoPath", (object?)s.PhotoPath ?? DBNull.Value);
            await cn.OpenAsync();
            var id = (int)await cmd.ExecuteScalarAsync();

            // Insert subjects if present
            if (s.Subjects != null && s.Subjects.Any())
            {
                foreach (var sub in s.Subjects)
                {
                    await using var cmdSub = new SqlCommand("INSERT INTO dbo.Student_Detail (StudentId, SubjectName) VALUES (@StudentId, @SubjectName)", cn);
                    cmdSub.Parameters.AddWithValue("@StudentId", id);
                    cmdSub.Parameters.AddWithValue("@SubjectName", sub.SubjectName);
                    await cmdSub.ExecuteNonQueryAsync();
                }
            }

            return id;
        }

        public async Task UpdateAsync(StudentMaster s)
        {
            const string sql = @"UPDATE dbo.Student_Master
                             SET Name=@Name, Age=@Age, Address=@Address, StateId=@StateId, PhoneNumber=@PhoneNumber, PhotoPath=@PhotoPath
                             WHERE StudentId=@StudentId";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Name", s.Name);
            cmd.Parameters.AddWithValue("@Age", s.Age);
            cmd.Parameters.AddWithValue("@Address", (object?)s.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StateId", s.StateId);
            cmd.Parameters.AddWithValue("@PhoneNumber", (object?)s.PhoneNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PhotoPath", (object?)s.PhotoPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StudentId", s.StudentId);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            // Simplest subjects update: delete existing and re-insert
            await using var cmdDel = new SqlCommand("DELETE FROM dbo.Student_Detail WHERE StudentId=@StudentId", cn);
            cmdDel.Parameters.AddWithValue("@StudentId", s.StudentId);
            await cmdDel.ExecuteNonQueryAsync();
            if (s.Subjects != null && s.Subjects.Any())
            {
                foreach (var sub in s.Subjects)
                {
                    await using var cmdSub = new SqlCommand("INSERT INTO dbo.Student_Detail (StudentId, SubjectName) VALUES (@StudentId, @SubjectName)", cn);
                    cmdSub.Parameters.AddWithValue("@StudentId", s.StudentId);
                    cmdSub.Parameters.AddWithValue("@SubjectName", sub.SubjectName);
                    await cmdSub.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM dbo.Student_Master WHERE StudentId = @Id";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<IEnumerable<StudentDetail>> GetSubjectsForStudentAsync(int studentId)
        {
            var list = new List<StudentDetail>();
            const string sql = "SELECT DetailId, SubjectName FROM dbo.Student_Detail WHERE StudentId = @StudentId";
            await using var cn = new SqlConnection(_conn);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@StudentId", studentId);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new StudentDetail { DetailId = rdr.GetInt32(0), StudentId = studentId, SubjectName = rdr.GetString(1) });
            }
            return list;
        }
    }
}
