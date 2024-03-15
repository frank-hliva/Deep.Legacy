[<AutoOpen>]
module App.Config

open Deep
open Deep.Routing
open Deep.Mvc
open Deep.View.DotLiquid

let registerRoutes (routes : routes) =
    Routes.Any(
        routes,
        "/?Controller/?Action/?Id",
        { Controller = "Home"; Action = "Index"; Id = "" }
    )

type RouteBuilder(config : RouteBuilderConfig) =
    inherit Routing.RouteBuilder(registerRoutes, config)

let requestKernelConfig (kernel : IKernel) =
    kernel

let config (kernel : IKernel) =
    let kernel =
        kernel
            .RegisterInstance(new Config())
            .Register<IView, View>(LifeTime.Singleton)
            .Register<AppInfoConfig>(LifeTime.Singleton)
            .Register<ServerConfig>(LifeTime.Singleton)
            .Register<StaticContentConfig>(LifeTime.Singleton)
            .Register<RouteBuilderConfig>(LifeTime.Singleton)
            .Register<ControllerConfig>(LifeTime.Singleton)
            .Register<ViewConfig>(LifeTime.Singleton)
            .Register<MemorySessionConfig>(LifeTime.Singleton)
            .Register<IRouteBuilder, RouteBuilder>(LifeTime.Singleton)
            .Register<Router>(LifeTime.Singleton)
            .Register<StaticContent>(LifeTime.Singleton)
            .Register<ISessionStore, MemorySessionStore>(LifeTime.Singleton)
    let listenerContainer =
        ListenerContainer()
            .Use(kernel.Resolve<StaticContent>())
            .Use(kernel.Resolve<Router>())
            .Use(ErrorHandler())
    kernel
        .RegisterInstance(listenerContainer)
        .RegisterInstance(new RequestKernelConfigurator(requestKernelConfig))