using System.Net;
using Biblioteca.Models;

namespace Biblioteca.Services;

public class HttpSecretariatClient(HttpClient http) : ISecretariatClient
{
    public async Task<EnrollmentStatus?> GetEnrollmentAsync(string matricol)
    {
        var response = await http.GetAsync($"enrollment/{Uri.EscapeDataString(matricol)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EnrollmentStatus>();
    }
}
