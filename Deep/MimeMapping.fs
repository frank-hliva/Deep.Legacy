module Deep.MimeMapping

open System.IO
open System.Web

let getMimeMapping (fileName) =
    match Path.GetExtension(fileName).ToLower() with
    | ".svg" -> "image/svg+xml"
    | ".webp" -> "image/webp"
    | _ -> MimeMapping.GetMimeMapping(fileName)