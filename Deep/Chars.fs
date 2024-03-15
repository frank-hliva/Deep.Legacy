[<AutoOpen>]
module Deep.CharsModule

open System

module Chars =

    let toString : char seq -> string = String.Concat

let (|StartsWith|_|) (pattern : string) (input : char seq) =
    let input = input |> Chars.toString
    if input.StartsWith(pattern) then 
        Some(pattern, input.[pattern.Length..])
    else None

let (|StartsWithAny|_|) (patterns : string list) (input : char seq) =
    patterns
    |> List.tryPick
        (fun pattern ->
            match input with
            | StartsWith pattern (h, t) -> Some(pattern, t)
            | _ -> None)