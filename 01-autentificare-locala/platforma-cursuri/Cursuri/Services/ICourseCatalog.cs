using Cursuri.Models;

namespace Cursuri.Services;

public interface ICourseCatalog
{
    IReadOnlyList<Course> GetAll();
    Course? FindByCode(string code);
}
