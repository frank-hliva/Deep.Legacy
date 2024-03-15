namespace Deep

open System
open System.Collections
open System.Collections.Generic
open System.Reflection
open Deep.Collections

type ValueObjectConverter(outputFilter : IDictionary<string, obj> -> obj) =

    let rec convert (values : obj) =
        match values with
        | null -> null
        | :? string -> values
        | :? IDictionary<string, obj> as dict ->
            dict
            |> Seq.map(fun kv -> kv.Key, kv.Value |> box |> convert)
            |> Map.ofSeq
            |> outputFilter
        | :? seq<string * obj> as s -> s |> Map.ofSeq |> convert
        | collection when collection |> ObjectType.isEnumerable ->
            let collection = collection :?> IEnumerable
            seq { for o in collection do yield (o |> convert) } |> box
        | value when value.GetType().IsValueType -> values
        | c when c.GetType().IsClass ->
            c |> objectToDict |> box |> convert
        | _ -> failwith "Invalid type"

    and objectToDict (o : obj) =
        let props = o.GetType().GetProperties(BindingFlags.Public ||| BindingFlags.Instance)
        props
        |> Seq.choose
            (fun p ->
                if p.CanRead && p.GetIndexParameters().Length = 0
                then
                    if o.GetType() = p.PropertyType then None
                    else
                        let value = try p.GetValue(o) with :? Exception as e -> null
                        Some(p.Name, value |> convert)
                else None) |> Map.ofSeq |> box

    member s.ConvertToDict(values : obj) = values |> convert

    new() = ValueObjectConverter(box)