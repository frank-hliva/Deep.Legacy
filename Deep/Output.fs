namespace Deep

open System
open System.IO
open System.IO.Compression
open System.Net

type IOutputProvider =
    abstract Flush : unit -> unit
    abstract OutputStream : Stream

type OutputProvider(response : HttpListenerResponse) =
    interface IOutputProvider with
        member p.Flush() = ()
        member p.OutputStream = response.OutputStream

type OutputBufferOutputProvider(response : HttpListenerResponse) =
    let outputBuffer = new MemoryStream()
    interface IOutputProvider with
        member p.Flush() =
            outputBuffer.CopyTo(response.OutputStream)
            outputBuffer.Flush()
            response.ContentLength64 <- outputBuffer.Length
        member p.OutputStream = response.OutputStream

type CompressionOutputProvider(response : HttpListenerResponse, acceptEncoding : string) =
    let outputBuffer = new MemoryStream()
    let compressionStream =
        match acceptEncoding with
        | "deflate" -> new DeflateStream(response.OutputStream, CompressionMode.Compress, false) :> Stream
        | "gzip" -> new GZipStream(response.OutputStream, CompressionMode.Compress, false) :> Stream
        | _ -> failwith "Invalid encoding"
    interface IOutputProvider with
        member p.Flush() =
            outputBuffer.CopyTo(compressionStream)
            outputBuffer.Flush()
            response.ContentLength64 <- outputBuffer.Length
        member p.OutputStream = compressionStream

type Output(request : HttpListenerRequest, response : HttpListenerResponse) =

    let mutable isDisposed = false

    let tryGetEncoding (acceptEncoding : string) =
        if (String.IsNullOrEmpty(acceptEncoding)) then None
        elif acceptEncoding.Contains("deflate") then Some("deflate")
        elif acceptEncoding.Contains("gzip") then Some("gzip")
        else None

    let acceptEncoding, outputProvider = 
        match request.Headers.["Accept-Encoding"] |> tryGetEncoding with
        | Some acceptEncoding ->
            Some(acceptEncoding), new CompressionOutputProvider(response, acceptEncoding)
            :> IOutputProvider
        | _ ->
        None, new OutputProvider(response)
        :> IOutputProvider

    interface IAutoDisposable with
        member f.IsDisposed with get() = isDisposed
        member f.Dispose() =
            outputProvider.Flush()
            isDisposed <- true

    member this.OutputStream
        with get() =
            if acceptEncoding.IsSome
            then response.AddHeader("Content-Encoding", acceptEncoding.Value) |> ignore
            outputProvider.OutputStream