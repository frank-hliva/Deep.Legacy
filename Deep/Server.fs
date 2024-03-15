module Deep.Server

open System
open System.Net
open System.Threading
open Deep.Log

let listen (uriPrefixes : string seq) (logger : ILogger) action =
    let listener = new HttpListener()
    logger.Log("Locations:") |> ignore
    listener.IgnoreWriteExceptions <- true
    uriPrefixes
    |> Seq.iter
        (fun uri ->
            listener.Prefixes.Add uri
            logger.LogWith($"{uri}").Status(Success "ADDED") |> ignore
        )
    logger.LogWith("Server started\t") |> ignore
    listener.Start()

    let rec loop () = async {
        let! context = Async.FromBeginEnd(listener.BeginGetContext, listener.EndGetContext)
        async { 
            let! result = Async.Catch(action context)
            match result with
            | Choice.Choice1Of2 _ -> ()
            | Choice.Choice2Of2 exn ->
                logger.LogError(exn.Message) |> ignore
                ()
        } |> Async.Start
        do! loop ()
    }
    logger.Status(LoggerStatuses.done') |> ignore
    loop () |> Async.Start