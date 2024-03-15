namespace Deep.Mvc

open Deep
open System.Net
open System.Reflection

type ControllerConfig(config : Config) =
    inherit AssemblyConfig()
    interface IConfigSection
    override c.GetAssemblyNames() =
        config.SelectAs<string[]>("Controllers.Assemblies")

module Controllers =
    let suffix = "Controller"

    let findAll (assemblies : Assembly[]) =
        assemblies
        |> Seq.collect(_.GetTypes())
        |> Seq.filter(_.Name.EndsWith(suffix))

    let tryFindByName (name : string) assemblies =
        assemblies
        |> findAll
        |> Seq.tryFind (fun c -> c.Name = name)

namespace Deep.Routing

open System
open System.Reflection
open Deep
open Deep.Mvc

type MvcDefaults = { Controller : string; Action : string; Id : string }

[<RequireQualifiedAccess>]
module internal MvcKeys =
    let Controller = "Controller"
    let Action = "Action"
    let Id = "Id"

type ControllerMethodType =
| Required = 0
| Optional = 1

type MvcRouteHandler() =

    let tryRegisterInt32Id (id : string) (container : IKernel) =
        match id.TryConvertToInt32() with
        | Some i -> container.RegisterInstance<Int32>(i)
        | _ -> container

    let tryRegisterDecimalId (id : string) (container : IKernel) =
        match id.TryConvertToDecimal() with
        | Some i -> container.RegisterInstance<Decimal>(i)
        | _ -> container

    let registerId (id : string) (container : IKernel) =
        container.RegisterInstance<string>(id)
        |> tryRegisterInt32Id id
        |> tryRegisterDecimalId id

    let rec findMethod (methodName : string) (httpMethod : string) (controllerType : Type) =
        controllerType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
        |> Seq.filter(fun mi -> mi.Name = methodName)
        |> Seq.tryFind
            (fun mi ->
                let routeAttr = mi.GetCustomAttribute<RouteAttribute>()
                match httpMethod with
                | "" -> routeAttr = null || routeAttr.HttpMethod = HttpMethods.Any
                | _ -> routeAttr <> null && routeAttr.HttpMethod = httpMethod)
        |> function
        | Some mi -> Some mi
        | None when httpMethod <> "" -> controllerType |> findMethod methodName ""
        | None -> None

    let rec allowedHttpMethods (methodName : string) (controllerType : Type) =
        let rec find acc = function
        | [] -> acc
        | (x : MethodInfo) :: xs -> 
            match x.GetCustomAttribute<RouteAttribute>() with
            | null -> [HttpMethods.Any]
            | routeAttr when routeAttr.HttpMethod = HttpMethods.Any ->
                [HttpMethods.Any]
            | routeAttr -> xs |> find (routeAttr.HttpMethod :: acc)
        controllerType.GetMethods(BindingFlags.Public ||| BindingFlags.Instance)
        |> List.ofArray
        |> List.filter(fun mi -> mi.Name = methodName)
        |> find []

    let replyAllowedHttpMethods (controllerType : Type) (actionName : string) (response : Reply) =
        response.StatusCode <- 204
        response.Response.Headers.Add("Access-Control-Allow-Headers: *")
        let httpMethods = String.Join(", ", controllerType |> allowedHttpMethods actionName)
        response.Response.Headers.Set
            ("Access-Control-Allow-Methods",
                if httpMethods = HttpMethods.Any then "*" else httpMethods)
        ()

    interface IRouteHandler with

        override h.InvokeAction (container : IKernel) =
            let request = container.Resolve<Request>()
            let parameters = request.Params |> Map.map(fun k v -> if k = MvcKeys.Controller || k = MvcKeys.Action then v |> Url.toPascalCase else v)
            (container.Resolve<ControllerConfig>() :> IAssemblyConfig).GetAssemblies()
            |> Controllers.tryFindByName (sprintf "%s%s" parameters.[MvcKeys.Controller] Controllers.suffix)
            |> function
            | Some controllerType -> async {
                let controller = container.Register(controllerType).Resolve(controllerType)
                let actionName = parameters.[MvcKeys.Action]
                for (methodName, methodType) in 
                    [
                        ("Loaded", ControllerMethodType.Optional)
                        ("BeforeAction", ControllerMethodType.Optional)
                        (actionName, ControllerMethodType.Required)
                        ("AfterAction", ControllerMethodType.Optional)
                    ]
                    do
                        match controllerType |> findMethod methodName request.HttpMethod with
                        | Some action ->
                            let container = container |> registerId parameters.[MvcKeys.Id]
                            let! result =
                                action
                                |> Function.invokeOn controller (container.RegisterInstance<IKernel> container)
                                |> RouteHandlerResult.toAsync
                                |> Async.Catch
                            match result with
                            | Choice.Choice1Of2 _ -> ()
                            | Choice.Choice2Of2 exn -> return raise(exn)
                        | _ ->
                            if request.HttpMethod = "OPTIONS" && methodName = actionName
                            then container.Resolve<Reply>() |> replyAllowedHttpMethods controllerType actionName
                            elif methodType = ControllerMethodType.Required
                            then return raise(HttpException(404, ""))
                            else () }
            | _ -> 
                let controllerName = parameters.[MvcKeys.Controller]
                let actionName = parameters.[MvcKeys.Action]
                async { return raise(HttpException(404, controllerName + "/" + actionName)) }

namespace Deep.Mvc

open Deep
open Deep.Routing
open System.Net

module Controller =
    
    let executeAction (kernel : IKernel) (path : string) = async {
        let items = path.Split [| '/' |]
        let controller, action, id =
            match items.Length with
            | 3 -> items.[0], items.[1], items.[2]
            | 2 -> items.[0], items.[1], ""
            | _ -> failwith "invalid path"
        let mvcRouteHandler = new MvcRouteHandler() :> IRouteHandler
        let context = kernel.Resolve<HttpListenerContext>()
        let routeParams =
            Map [
                MvcKeys.Controller, controller
                MvcKeys.Action, action
                MvcKeys.Id, id
            ]
        let kernel = 
            kernel
                .RegisterInstance<Request>(new Request(context.Request, routeParams))
                .Register<Reply>(LifeTime.Singleton)
        do! kernel |> mvcRouteHandler.InvokeAction
        kernel |> AutoDisposer.disposeObjects }