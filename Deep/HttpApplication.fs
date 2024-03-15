namespace Deep

open Deep
open Deep.Routing
open System
open System.Net
open System.IO
open Deep.Log

[<AllowNullLiteral>]
type RequestKernelConfigurator(config : IKernel -> IKernel) =
    member c.Config = config

type HttpApplication(applicationKernel : IKernel, listenerContainer : ListenerContainer, requestConfigurator : RequestKernelConfigurator) =
    let serverConfig = applicationKernel.Resolve<ServerConfig>().GetServerOptions()
    let headers = serverConfig.Headers
    abstract RegisterRequestObjects : HttpListenerContext -> IKernel -> IKernel
    default a.RegisterRequestObjects (context : HttpListenerContext) (requestContainer : IKernel) =
        let output = new Output(context.Request, context.Response)
        [
            context |> box
            output |> box
            new Request(context.Request) |> box
            new Response(context.Response, output, headers) |> box
        ]
        |> Seq.fold
            (fun (requestContainer : IKernel) (instance : obj) ->
                instance |> requestContainer.RegisterInstance) requestContainer
        |> fun requestContainer ->
            requestContainer
                .Register<Get>(LifeTime.Singleton)
                .Register<Post>(LifeTime.Singleton)
                .Register<Posted>(LifeTime.Singleton)
                .Register<MultipartForm>(LifeTime.Singleton)
                .Register<Reply>(LifeTime.Singleton)
                .Register<ISessionManager, SessionManager>(LifeTime.PerResolve)
                .Register<FlashMessages, FlashMessages>(LifeTime.PerResolve)
        |> fun kernel -> 
            match requestConfigurator with
            | null -> kernel
            | _ -> requestConfigurator.Config(kernel)

    member a.Container = applicationKernel

    member a.Listener(context : HttpListenerContext) =
        let kernel = new Kernel(applicationKernel) :> IKernel |> a.RegisterRequestObjects context
        listenerContainer.Apply(kernel)

    interface IApplication with
        override a.Run(uriPrefixes : string seq) =
            Server.listen uriPrefixes (applicationKernel.Resolve<ILogger>()) a.Listener

    new(applicationKernel, router) = HttpApplication(applicationKernel, router, null)