using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WatsonWebserver;

namespace gallery;

public class Program
{
    static void Main(string[] args)
    {
        Server server = new Server("127.0.0.1", 8080, false, DefaultRoute);
        server.Routes.Content.Add("/", true);
        server.Routes.Content.BaseDirectory = "www";
        server.Start();

        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();
    }

    [StaticRoute(WatsonWebserver.HttpMethod.GET, "/hello")]
    public static async Task GetHelloRoute(HttpContext ctx)
    {
        await ctx.Response.Send("Hello from the GET /hello static route!");
    }

    [ParameterRoute(WatsonWebserver.HttpMethod.POST, "/{version}/bar")]
    public static async Task PostBarRoute(HttpContext ctx)
    {
        await ctx.Response.Send("Hello from the POST /" + ctx.Request.Url.Parameters["version"] + "/bar parameter route!");
    }

    [DynamicRoute(WatsonWebserver.HttpMethod.GET, "^/foo/\\d+$")]
    public static async Task GetFooWithId(HttpContext ctx)
    {
        await ctx.Response.Send("Hello from the GET /foo/[id] dynamic route!");
    }

    [DynamicRoute(WatsonWebserver.HttpMethod.GET, "^/foo/")]
    public static async Task GetFoo(HttpContext ctx)
    {
        await ctx.Response.Send("Hello from the GET /foo/ dynamic route!");
    }

    static async Task DefaultRoute(HttpContext ctx)
    {
        await ctx.Response.Send("Hello from the default route!");
    }
}