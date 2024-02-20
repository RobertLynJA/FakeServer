
namespace FakeServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        Server server = new(8088);

        await server.Run();
    }
}
