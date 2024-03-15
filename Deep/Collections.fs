namespace Deep.Collections

open System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Reflection

module ObjectType =
    let isEnumerable (o : obj) =
        o.GetType().GetInterfaces()
        |> Array.exists
            (fun i ->
                i.IsGenericType &&
                i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>)

module Map =
    
    let ofDict dictionary = 
        (dictionary :> seq<_>)
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq

    let toDict (map : Map<'a, 'b>) = map :> IDictionary<'a, 'b>

    let addMap (map2 : Map<_, _>) (map1 : Map<_, _>) =
        map2 |> Seq.fold(fun acc kv -> acc |> Map.add kv.Key kv.Value) map1

type MemberList =
| WhiteList of string list
| BlackList of string list
| All

type Dict() =

    static let whiteListChooser (o : obj) (whiteList : Set<string>) (prop : PropertyInfo) =
        if whiteList.Contains prop.Name then
            let value = try prop.GetValue o with :? Exception -> null
            Some(prop.Name, value)
        else None

    static let blackListChooser (o : obj) (blackList : Set<string>) (prop : PropertyInfo) =
        if blackList.Contains prop.Name then None
        else 
            let value = try prop.GetValue o with :? Exception -> null
            Some(prop.Name, value)

    static let allChooser (o : obj) (prop : PropertyInfo) = Some(prop.Name, prop.GetValue o)

    static member From(o : obj, ?memberList : MemberList) =
        match o with
        | null -> null
        | o ->
            let chooser =
                match defaultArg memberList All with
                | WhiteList whitelist -> whiteListChooser o (Set whitelist)
                | BlackList blacklist -> blackListChooser o (Set blacklist)
                | All -> allChooser o 
            o.GetType().GetProperties(BindingFlags.Public ||| BindingFlags.Instance)
            |> Seq.choose(chooser) |> dict