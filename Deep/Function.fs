module Deep.Function

open System
open System.Reflection

let getMethodInfo (func : obj) =
    func.GetType().GetMethods()
    |> Seq.filter(fun mi -> mi.Name = "Invoke")
    |> Seq.map(fun mi -> mi, mi.GetParameters())
    |> Seq.maxBy(fun (mi, p) -> p.Length)

let findByAttribute<'t> (assemblies : Assembly seq) =
    assemblies
    |> Seq.collect(_.GetTypes())
    |> Seq.collect(_.GetMethods())
    |> Seq.filter(fun m -> m.GetCustomAttributes(typedefof<'t>, false).Length > 0)

let getAttribute<'t> (m : MethodInfo) =
    m.GetCustomAttributes(typedefof<'t>, false) |> Seq.head :?> 't

let tryGetAttribute<'t> (m : MethodInfo) =
    let attrs = m.GetCustomAttributes(typedefof<'t>, false)
    match attrs.Length with
    | 0 -> None
    | _ -> Some(attrs |> Seq.head :?> 't)

module private Creator =

    let returnType creator =
        let args = creator.GetType().BaseType.GenericTypeArguments
        args.[args.Length - 1]

    let invoke creator =
        (creator |> getMethodInfo |> fst).Invoke(creator, [| null |])

let invokeOn targetType (kernel : IKernel) func =
    let methodInfo, parameterInfos = 
        match box func with
        | :? MethodInfo as methodInfo ->
            methodInfo, methodInfo.GetParameters()
        | _ -> func |> getMethodInfo
    let toParam (paramInfo : ParameterInfo) =
        kernel.Resolve(paramInfo.ParameterType)
    methodInfo.Invoke(targetType, parameterInfos |> Array.map toParam)

let invoke (kernel : IKernel) func =
    func |> invokeOn func kernel