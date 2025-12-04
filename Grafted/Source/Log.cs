namespace Grafted;

public static class Log {
    public static void Info(string text) {
        Console.WriteLine(text);
    }

    public static void Warning(string text, int max = 100) {

        Console.WriteLine("WARNING - " + text);
    }

    public static void Error(string text) {
        Console.WriteLine("ERROR - " + text);
    }

    public static void Error(Exception exception) {
        Console.WriteLine(exception);
    }

    public static void Debug(string text) {

        return;
        //Console.WriteLine("Debug - " + text);
    }
}