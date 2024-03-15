namespace Deep

open System
open Deep
open Deep.Routing
open Deep.Log

type [<AbstractClass>] Booter(kernel : IKernel) =
    let mutable container =
        kernel.Register<ILogger, ConsoleLogger>(LifeTime.Singleton)

    abstract DefaultConfigurator : IKernel -> IKernel
    member b.Kernel with get() = container
    member b.Config() =
        container <- b.DefaultConfigurator(container)
        container <- container.RegisterInstance<IKernel>(container)
    member b.Config(configurator : IKernel -> IKernel) =
        container <- configurator(container)
        b.Config()
    abstract Boot : unit -> unit
    abstract Boot : uriPrefixes : string seq -> unit

type ApplicationBooter<'t when 't : not struct and 't :> IApplication>(kernel) =
    inherit Booter(kernel)
    override b.DefaultConfigurator(kernel) =
        kernel.Register<'t>(LifeTime.Singleton)
    override b.Boot() =
        b.Boot(b.Kernel.Resolve<ServerConfig>().GetServerOptions().UriPrefixes)
    override b.Boot(uriPrefixes : string seq) =
        b.Kernel.Resolve<'t>().Run(uriPrefixes)