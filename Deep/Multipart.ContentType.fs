module Deep.Multipart.ContentType

open System.Text
open System.Net.Mime

let isTextContentType (contentType : ContentType) =
    match contentType.MediaType with
    | "text/plain" | "text/html" | "text/xml"
    | "application/xml" | "application/json"
    | "text/csv" -> true
    | _ -> false

let tryGetEncoding (contentType : ContentType) =
    match contentType with
    | null -> None
    | contentType ->
        match contentType.CharSet with
        | null when isTextContentType contentType ->
            Encoding.UTF8 |> Some
        | null -> None
        | encoding -> encoding |> Encoding.GetEncoding |> Some

let encodeData (encoding : Encoding) (data : string) =
    data |> Encoding.Default.GetBytes |> encoding.GetString
