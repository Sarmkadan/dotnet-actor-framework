using System;
public record Test
{
    public Guid Id { get; init; }
    public Test(Guid id)
    {
        Id = id;
    }
}
public class Program
{
    public static void Main()
    {
        var t = new Test(Guid.NewGuid());
        Console.WriteLine(t.Id);
    }
}
