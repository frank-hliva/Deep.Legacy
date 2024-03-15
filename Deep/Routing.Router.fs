namespace Deep.Routing

open Deep
open System
open System.Net

type Router(routeBuilder : IRouteBuilder) =

    let delimiter = '/'
    let paramPrefix = ":"
    let optionalParamPrefix = "?"

    let parseByPrefix (prefix : string) (input : string) =
        if input.StartsWith(prefix)
        then Some(input.[1..])
        else None

    let (|Param|_|) (input : string) = input |> parseByPrefix paramPrefix
    let (|OptionalParam|_|) (input : string) = input |> parseByPrefix optionalParamPrefix

    let rec parseItems (i : int) (acc : RouteParams) (urlItems : string[]) = function
    | [] -> Some acc
    | x :: xs ->
        match (x : string) with
        | Param p ->
            if urlItems.Length - 1 < i then None
            else xs |> parseItems (i + 1) (acc.Add(p, urlItems.[i])) urlItems
        | OptionalParam p ->
            let value = if urlItems.Length - 1 < i then "" else urlItems.[i]
            xs |> parseItems (i + 1) (acc.Add(p, value)) urlItems
        | x ->
            if urlItems.Length - 1 < i || x <> urlItems.[i] then None
            else xs |> parseItems (i + 1) acc urlItems

    let matchRoute (urlItems : string[]) (patternItems : string[]) =
        if urlItems.Length > patternItems.Length then None
        else patternItems |> List.ofSeq |> parseItems 0 Map.empty urlItems

    let addDefaults (defaults : RouteDefaults) (parameters : RouteParams) =
        defaults
        |> Map.fold
            (fun (acc : Map<string, string>) k v ->
                match acc.TryFind k with
                | Some value when String.IsNullOrEmpty value -> acc.Add(k, v)
                | None -> acc.Add(k, v)
                | _ -> acc) parameters

    let matchChooser urlItems httpMethod' (route : route) =
        match route.Pattern.Split [| delimiter |] |> matchRoute urlItems with
        | Some parameters when route.HttpMethod = HttpMethods.Any || route.HttpMethod = httpMethod' ->
            {
                Handler = route.Handler
                Parameters =
                    match route.Filter with
                    | Some filter -> filter(parameters)
                    | _ -> parameters |> addDefaults route.Defaults
            } |> Some
        | _ -> None

    member r.Match (httpMethod : string) (url : string) =
        let urlItems = (url |> Url.removeQueryString).Split [| delimiter |]
        routeBuilder.Routes
        |> List.sortByDescending (_.Priority)
        |> List.tryPick (matchChooser urlItems httpMethod)

    interface IListener with

        member r.Listen (request : Request) (response : Response) (kernel : IKernel) (e : exn option) = async {
            let context = kernel.Resolve<HttpListenerContext>()
            match r.Match request.HttpMethod request.RawUrl with
            | Some result ->
                let request = new Request(context.Request, result.Parameters)
                let kernel = kernel.RegisterInstance(request)
                let! result = result.Handler.InvokeAction kernel |> Async.Catch
                match result with
                | Choice1Of2 _ ->
                    kernel |> AutoDisposer.disposeObjects
                    return ListenerResult.End
                | Choice2Of2 exn ->
                    return ListenerResult.Error exn
            | _ -> return ListenerResult.Next }