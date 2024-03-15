namespace Deep.Multipart

open System
open System.IO
open System.Text
open System.Collections.Generic

type MultipartForm(input : string) =

    let readToMax (string : string) (max : int) (input : string) =
        let index = input.IndexOf(string)
        if index = - 1 || index > max then None
        else Some(input.[0..index - 1], input.[index + string.Length..])

    let readTo (string : string) (input : string) =
        let index = input.IndexOf(string)
        if index = - 1 then None
        else Some(input.[0..index - 1], input.[index + string.Length..])

    let getName (index : int) (info : Info) =
        match info.ContentDisposition with
        | null -> index.ToString()
        | _ -> info.ContentDisposition.Name

    let rec parseItems index acc boundary (input : string) =
        match input |> readTo (Definitions.lineEnd + Definitions.lineEnd) with
        | Some (info, tail) -> 
            match tail |> readTo (Definitions.lineEnd + boundary) with
            | Some (data, tail) ->
                let info = info.Trim() |> ItemInfo.parse
                let data =
                    match info.ContentType |> ContentType.tryGetEncoding with
                    | Some encoding -> data |> ContentType.encodeData encoding
                    | _ -> data
                let item = { Info = info; Data = data }
                tail |> parseItems (index + 1) ((info |> getName index, item) :: acc) boundary
            | _ -> acc
        | _ -> acc

    let parse (input : string) =
        match input |> readToMax Definitions.lineEnd 75 with
        | Some (boundary, tail) ->
            tail |> parseItems 0 [] boundary |> List.rev
        | _ -> []

    let data = 
        input
        |> parse
        |> List.groupBy(fun (name, item) -> name)
        |> List.map(fun (name, item) -> name, item |> Seq.map(snd))

    let map = data |> Map

    member f.Fields = map
    member f.Item with get(index : int) = data.[index]
    member f.Item with get(name : string) = map.[name]

    new(stream : Stream) =
        MultipartForm((new StreamReader(stream, Encoding.Default)).ReadToEnd())