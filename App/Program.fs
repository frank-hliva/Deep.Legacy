module App.Application

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
    booter.Boot("http://127.0.0.1:3000/")
    Console.WriteLine("Server running on port 3000...")
    Console.ReadKey() |> ignore
    0