module Deep.Props.Object

open System
open System.Reflection
open Deep
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Collections.Generic

let getterCache = new ConcurrentDictionary<Type, Func<obj, Dictionary<string, obj>>>()

let private toDictFactory (objType : Type) =
    let dict = Expression.Variable(typeof<Dictionary<string, obj>>);

    let inputExpression = Expression.Parameter(typeof<obj>, "input")
    let typedInputExpression = Expression.Convert(inputExpression, objType)

    let dictType = typeof<Dictionary<string, obj>>
    let add = dictType.GetMethod("Add", BindingFlags.Public ||| BindingFlags.Instance, null, [| typeof<string>; typeof<obj> |], null)

    let body = new List<Expression>()
    body.Add(Expression.Assign(dict, Expression.New(typeof<Dictionary<string, obj>>)))

    let props = objType.GetTypeInfo().GetProperties(BindingFlags.Public ||| BindingFlags.Instance)
    for p in props do
        if p.CanRead && p.GetIndexParameters().Length = 0 && (not <| p.DeclaringType.Namespace.EndsWith(".DynamicProxies")) then
            let key = Expression.Constant(p.Name)
            let value = Expression.Property(typedInputExpression, p)
            let valueAsObject = Expression.Convert(value, typeof<obj>)
            body.Add(Expression.Call(dict, add, key, valueAsObject))
    body.Add(dict)
    let block = Expression.Block([| dict |], body);

    let lambda = Expression.Lambda<Func<obj, Dictionary<string, obj>>>(block, inputExpression);
    lambda.Compile()

let toDict (o : obj) =
    match o with
    | null -> null
    | value ->
        let t = value.GetType()
        let getter = getterCache.GetOrAdd(t, new Func<Type, Func<obj, Dictionary<string, obj>>>(fun _ -> toDictFactory t))
        o |> getter.Invoke

let toSeq (o : obj) =
    match o |> toDict with
    | null -> null
    | value -> value |> Seq.map(fun x -> x.Key, x.Value)

let toMap (o : 't) =
    o |> toSeq |> Map.ofSeq

let assignSeq (o : 't, props: seq<string * obj>) =
    let objType = o.GetType()
    props
    |> Seq.iter
        (fun (key, value) ->
            let prop = objType.GetProperty(key)
            let value =
                match value with
                | null -> null
                | _ -> value |> TypeConversion.changeType prop.PropertyType
            prop.SetValue(o, value))
    o

let assignMap (o : 't, props: Map<string, obj>) =
    assignSeq(o, props |> Map.toSeq)