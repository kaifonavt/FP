using System.Text.Json;

namespace SupportTicket;

public static class UserParser
{
    public static List<User> ParseUsersFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File '{filePath}' not found.");
        }
        string jsonUsers = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<User>>(jsonUsers) ?? new List<User>();
    }
}