namespace Deep.Multipart

open System.Net.Mime
open System.Net.Http.Headers

module Definitions =
    let lineEnd = "\r\n"

type Info =
    {
        ContentDisposition : ContentDispositionHeaderValue
        ContentType : ContentType
    }

type Item = { Info : Info; Data : string }