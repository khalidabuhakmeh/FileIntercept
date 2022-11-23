namespace FileIntercept;

public class Database
{
    public Stream? GetFileByUserId(string userId)
    {
        if (userId == null) throw new ArgumentNullException(nameof(userId));
        
        // just reading embedded resources
        return typeof(Database).Assembly
            .GetManifestResourceStream(userId);
    }
    
}