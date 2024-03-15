# Deep Legacy

MVC Web framework and server written in F# lang. for .NET Framework 4.8
Version: *3.0.1*

## License:
BSD-4-Clause: https://github.com/frank-hliva/Deep/blob/master/LICENSE.md

## Example:

```fsharp
open Deep
open Deep.Routing
open System

[<Get("/test/?param")>]
let hello (req : Request) (reply : Reply) =
    reply.Writer
    |> wprintf "Hello <strong>World!</strong> %s" req.Params.["param"]
    |> wprintf "________________________________"

[<EntryPoint>]
let main argv =
    let booter = new ApplicationBooter<HttpApplication>(new Kernel())
    booter.Config(config)
    booter.Boot()
    Console.WriteLine("Server running...")
    Console.ReadKey() |> ignore
    0
```

## Controller example:

```fs
type HomeController(reply : Reply) =
    inherit FrontendController()

    member c.Index(flashMessages : FlashMessages) = async {
        c.Title <- "Index"
        do! flashMessages.Send("Flash message")
        reply.View ["Name" => "world"]
    }

    member c.LearnMore(sessions : ISessionManager) = async {
        c.Title <- "Learn more"
        let! counter = sessions.GetItemOrDefault<int>("counter")
        do! sessions.SetItem("counter", counter + 1)
        let! counter = sessions.GetItem<int>("counter")
        reply.ViewData.["counter"] <- counter
        reply.View()
    }
```
