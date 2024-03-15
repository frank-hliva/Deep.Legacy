module Deep.Url

open System
open System.Net

let removeQueryString (url : string) =
    match url.IndexOf "?" with
    | -1 -> url
    | pos -> url.[0..pos - 1]

let toPascalCase (uri : string) =
    let len = uri.Length
    let rec loop upper i (acc : char list) = 
        if i = len then acc |> String.Concat
        else
            match uri.[i] with
            | '-' -> loop true (i + 1) acc
            | c -> loop false (i + 1) (acc @ [(if upper then Char.ToUpper(c) else c)])
    loop true 0 []