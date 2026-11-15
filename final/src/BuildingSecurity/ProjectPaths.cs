namespace BuildingSecurity;

public static class ProjectPaths
{
    // Start next to the running assembly so the caller's working folder does not matter.
    public static string Root
    {
        get
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder is not null)
            {
                if (File.Exists(Path.Combine(folder.FullName, "BuildingSecuritySystem.sln")))
                    return folder.FullName;
                folder = folder.Parent;
            }
            throw new DirectoryNotFoundException("Project folder not found. Run the code from the extracted BuildingSecuritySystem project.");
        }
    }
}
