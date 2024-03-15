module Deep.Headers

open System
open System.Net

let addCharset (response : Response) (headers : WebHeaderCollection) =
    let contentEncoding = response.ContentEncoding
    if contentEncoding <> null then
        let charset = contentEncoding.HeaderName
        if String.IsNullOrEmpty(charset) |> not then
            let contentType = response.ContentType
            headers.Remove("Content-Type")
            headers.Add(
                "Content-Type",
                sprintf "%s; charset=%s" contentType charset
            )