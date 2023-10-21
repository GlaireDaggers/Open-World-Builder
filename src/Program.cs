namespace OpenWorldBuilder
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            using (var app = new App())
            {
                app.Run();
            }
        }
    }
}