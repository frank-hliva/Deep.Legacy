[<AutoOpen>]
module Deep.Writer

open System
open System.IO
open System.IO.Compression

let private printToWriter (value : string) (writer : TextWriter) =
    writer.Write(value)
    writer

let private printToWriterLineEnd (value : string) (writer : TextWriter) =
    writer.Write(value + Environment.NewLine)
    writer

let wprintf format = Printf.ksprintf printToWriter format
let wprintfn format = Printf.ksprintf printToWriterLineEnd format