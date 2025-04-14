




using OpenGL;

class Program
{
    static void Main(string[] args)
    {
        using (Game game = new Game(1600, 900))
        {
            game.Run();
        }
    }
}

