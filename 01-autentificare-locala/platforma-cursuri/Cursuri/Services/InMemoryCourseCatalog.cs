using Cursuri.Models;

namespace Cursuri.Services;

public class InMemoryCourseCatalog : ICourseCatalog
{
    private readonly List<Course> courses =
    [
        new("TASCD", "Tehnologii pentru asigurarea securității și confidențialității datelor", "Andreea Vasile", 1, 5),
        new("PAW", "Programarea aplicațiilor Windows", "Victor Neagu", 1, 5),
        new("BD", "Baze de date", "Andreea Vasile", 2, 6),
        new("RC", "Rețele de calculatoare", "Victor Neagu", 2, 5),
    ];

    public InMemoryCourseCatalog()
    {
        Console.WriteLine("InMemoryCourseCatalog: instanță nouă");
    }

    public IReadOnlyList<Course> GetAll() => courses;

    public Course? FindByCode(string code) =>
        courses.FirstOrDefault(c => c.Code == code);
}
