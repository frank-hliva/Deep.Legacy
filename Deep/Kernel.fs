namespace Deep

open System
open System.Reflection

type LifeTime =
| PerResolve = 0
| Singleton = 1

type IExternalResolver =
    abstract Contains : Type -> bool
    abstract Resolve : Type -> obj

type IKernel =
    abstract Register : Type * Type * ?lifeTime : LifeTime -> IKernel
    abstract Register : Type * ?lifeTime : LifeTime -> IKernel
    abstract Register<'t1, 't2> : ?lifeTime : LifeTime -> IKernel
    abstract Register<'t> : ?lifeTime : LifeTime -> IKernel
    abstract RegisterInstance : Type * obj -> IKernel
    abstract RegisterInstance : obj -> IKernel
    abstract RegisterInstance<'t> : obj -> IKernel
    abstract Resolve : Type -> obj
    abstract Resolve<'t> : unit -> 't
    abstract GetOrResolve : Type -> obj
    abstract GetOrResolve<'t> : unit -> 't
    abstract Contains : Type -> bool
    abstract TryFindInstance : Type -> obj option

type ExternalResolver(container : IKernel) =
    interface IExternalResolver with
        member r.Contains(t) = container.Contains(t)
        member r.Resolve(t) = container.Resolve(t)

type internal KernelItem =
    {
        Abstraction : Type
        ImplementedBy : Type option
        LifeTime : LifeTime
        mutable Instance : obj option ref
    }

type private KernelMap = Map<string, KernelItem>

type private SearchResult =
| Internal of KernelItem
| External of obj

type Kernel internal (types : KernelMap, externalResolver : IExternalResolver option) =

    let containsType (t : Type) =
        if types.ContainsKey(t.AssemblyQualifiedName) then true
        else
            match externalResolver with
            | Some resolver -> resolver.Contains t
            | _ -> false

    let containsParamType (p : ParameterInfo) =
        p.ParameterType |> containsType

    let find (t : Type) (types : KernelMap) =
        match types |> Map.tryFind t.AssemblyQualifiedName with
        | Some kernelItem -> Internal(kernelItem)
        | _ ->
            match externalResolver with
            | Some resolver -> External(resolver.Resolve(t))
            | _ -> failwith (sprintf "Type %s is not registered" t.Name)

    let chooseParams (c : ConstructorInfo) =
        let parameters = c.GetParameters()
        if parameters |> Seq.forall(containsParamType)
        then Some(parameters)
        else None

    let rec toParam (p : ParameterInfo) =
        let t = p.ParameterType
        if types.ContainsKey(t.AssemblyQualifiedName)
        then t |> resolve
        else externalResolver.Value.Resolve(t)

    and resolve (t : Type) =
        match types |> find t with
        | External o -> o
        | Internal i when (!i.Instance).IsSome -> (!i.Instance).Value
        | Internal i ->
            let implementation = i.ImplementedBy.Value
            implementation.GetConstructors()
            |> Seq.sortByDescending(_.GetParameters().Length)
            |> Seq.tryPick(chooseParams)
            |> function
            | Some parameterInfos ->
                let instance = Activator.CreateInstance(implementation, parameterInfos |> Array.map(toParam))
                match i.LifeTime with
                | LifeTime.PerResolve -> instance
                | LifeTime.Singleton ->
                    lock i.Instance (fun () -> i.Instance := Some instance)
                    instance
                | _ -> failwith "Invalid lifetime"
            | _ -> failwith (sprintf "Constructor parameter mismatch on type: %s" implementation.FullName)

    interface IKernel with

        member k.Register(abstraction : Type, implementedBy : Type, ?lifeTime : LifeTime) =
            new Kernel(types |> Map.add abstraction.AssemblyQualifiedName {
                Abstraction = abstraction
                ImplementedBy = Some implementedBy
                LifeTime = defaultArg lifeTime LifeTime.PerResolve
                Instance = ref None
            }, externalResolver) :> IKernel

        member k.Register(implementedBy : Type, ?lifeTime : LifeTime) =
            new Kernel(types |> Map.add implementedBy.AssemblyQualifiedName {
                Abstraction = implementedBy
                ImplementedBy = Some implementedBy
                LifeTime = defaultArg lifeTime LifeTime.PerResolve
                Instance = ref None
            }, externalResolver) :> IKernel

        member k.Register<'t1, 't2>(?lifeTime : LifeTime) =
            let abstraction = typedefof<'t1>
            let implementedBy = typedefof<'t2>
            new Kernel(types |> Map.add abstraction.AssemblyQualifiedName {
                Abstraction = abstraction
                ImplementedBy = Some implementedBy
                LifeTime = defaultArg lifeTime LifeTime.PerResolve
                Instance = ref None
            }, externalResolver) :> IKernel

        member k.Register<'t>(?lifeTime : LifeTime) =
            let implementedBy = typedefof<'t>
            new Kernel(types |> Map.add implementedBy.AssemblyQualifiedName {
                Abstraction = implementedBy
                ImplementedBy = Some implementedBy
                LifeTime = defaultArg lifeTime LifeTime.PerResolve
                Instance = ref None
            }, externalResolver) :> IKernel

        member k.RegisterInstance(abstraction : Type, instance : obj) =
            new Kernel(types |> Map.add abstraction.AssemblyQualifiedName {
                Abstraction = abstraction
                ImplementedBy = None
                LifeTime = LifeTime.Singleton
                Instance = ref <| Some instance
            }, externalResolver) :> IKernel

        member k.RegisterInstance<'t>(instance : obj) =
            let abstraction = typedefof<'t>
            new Kernel(types |> Map.add abstraction.AssemblyQualifiedName {
                Abstraction = abstraction
                ImplementedBy = None
                LifeTime = LifeTime.Singleton
                Instance = ref <| Some instance
            }, externalResolver) :> IKernel

        member k.RegisterInstance(instance : obj) =
            let abstraction = instance.GetType()
            new Kernel(types |> Map.add abstraction.AssemblyQualifiedName {
                Abstraction = abstraction
                ImplementedBy = None
                LifeTime = LifeTime.Singleton
                Instance = ref <| Some instance
            }, externalResolver) :> IKernel

        member k.Resolve(t : Type) = t |> resolve

        member k.Resolve<'t>() =
            (k :> IKernel).Resolve(typedefof<'t>) :?> 't

        member k.GetOrResolve(t : Type) =
            match t |> (k :> IKernel).TryFindInstance with
            | Some instance -> instance
            | _ -> (k :> IKernel).Resolve(t)

        member k.GetOrResolve<'t>() =
            (k :> IKernel).GetOrResolve(typedefof<'t>) :?> 't

        member k.Contains(t) = t |> containsType

        member k.TryFindInstance(t : Type) =
            match types |> Map.tryFind t.AssemblyQualifiedName with
            | Some kernelItem when kernelItem.Instance.Value.IsSome ->
                Some kernelItem.Instance.Value.Value
            | _ -> None

    new() = Kernel(Map.empty, None)
    new(resolver : IExternalResolver) = Kernel(Map.empty, Some resolver)
    new(resolver : IKernel) = Kernel(new ExternalResolver(resolver))

    static member Create() = new Kernel() :> IKernel
    static member Create(resolver : IExternalResolver) = new Kernel(resolver) :> IKernel
    static member Create(resolver : IKernel) = new Kernel(resolver) :> IKernel