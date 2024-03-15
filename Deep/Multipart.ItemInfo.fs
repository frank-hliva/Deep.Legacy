module Deep.Multipart.ItemInfo

open System
open System.Net.Mime
open System.Net.Http.Headers

let private parseHeadersToMap (info : string) =
    info.Split ([| Definitions.lineEnd |], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.filter(_.Contains(":"))
    |> Seq.map
        (fun l ->
            let p = l.IndexOf(":")
            l.[0..p - 1].Trim().ToLower(), l.[p + 1..].Trim())
    |> Map

let parse (info : string) =
    let map = info |> parseHeadersToMap
    {
        ContentDisposition = 
            match map.TryFind("content-disposition") with
            | Some value ->
                let cd = ContentDispositionHeaderValue.Parse(value)
                if cd.Name <> null then cd.Name <- cd.Name.Trim('\"')
                if cd.FileName <> null then cd.FileName <- cd.FileName.Trim('\"')
                if cd.FileNameStar <> null then cd.FileNameStar <- cd.FileNameStar.Trim('\"')
                cd
            | _ -> null
        ContentType =
            match map.TryFind("content-type") with
            | Some value -> ContentType(value)
            | _ -> null
    }