public static class CinematicTestLog
{
    public static bool Error(string text)
    {
        Errors.Add(text);
        Console.WriteLine("NATIVE ERROR " + text);
        return false;
    }

    public static readonly List<string> Errors = new();
}
