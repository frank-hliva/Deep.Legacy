namespace Deep

open Deep
open Deep.Mvc
open System

type ErrorHandler() =

    let toHttpStatusCode (e : exn) =
        match e with
        | :? HttpException as e ->
            match e.Data0 with
            | 403 | 404 | 500 -> e.Data0
            | _ -> 500
        | _ -> 500

    interface IListener with

        member r.Listen (request : Request) (response : Response) (kernel : IKernel) (exn : exn option) = async {
            match exn with
            | Some exn ->
                let action = sprintf "Error/Page%d" (exn |> toHttpStatusCode)
                do! action |> Controller.executeAction kernel
                return ListenerResult.End 
            | _ -> return ListenerResult.Next }

type ApiErrorHandler() =

    interface IListener with

        member r.Listen (request : Request) (response : Response) (kernel : IKernel) (exn : exn option) = async {
            match exn with
            | Some exn ->
                let action = "Error/Api"
                do! action |> Controller.executeAction (kernel.RegisterInstance<exn>(exn))
                return ListenerResult.End 
            | _ -> return ListenerResult.Next }