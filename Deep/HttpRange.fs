module Deep.HttpRange

open System
open Deep

let isRangeContent = function
| StartsWithAny ["audio/"; "video/"] _ -> true
| _ -> false

module RangeHeader =

    let private bytesPrefix = "bytes="

    let private tryParseInt64 (input : string) =
        let ok, value = input |> Int64.TryParse
        if ok then Some value
        else None

    let tryParse = function
    | null -> None
    | (input : string) when input.StartsWith(bytesPrefix, StringComparison.OrdinalIgnoreCase) && input.Contains("-") ->
        let items = input.[bytesPrefix.Length..].Split [|'-'|] |> Array.map(tryParseInt64)
        if items.Length = 2 && items |> Seq.forall Option.isSome
        then Some(items.[0].Value, items.[1].Value)
        else None
    | _ -> None