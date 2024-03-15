namespace Deep

open System
open System.IO
open System.Text
open System.Net
open System.Web
open System.Collections.Generic
open Newtonsoft.Json
open Deep.IO
open Deep.Collections

type Reply(request : Request, response : Response, staticContentOptions : StaticContentOptions, view : IView) =
    let writer = response.GetWriter()
    let mutable isDisposed = false
    let viewData = new Dictionary<string, obj>()
    do
        viewData.Add("Url", request.RawUrl)
        viewData.Add("Root", request.Root)
    let combineViewData (viewData : ViewData option) (replyViewData : IDictionary<string, obj>) =
        (defaultArg viewData Map.empty)
        |> Map.addMap (replyViewData |> Map.ofDict)
    static let printToReply (value : string) (reply : Reply) =
        reply.Writer.Write(value)
        reply
    static let printToReplyLineEnd (value : string) (reply : Reply) =
        reply.Writer.Write(value + Environment.NewLine)
        reply
    do response.ContentType <- ContentTypes.html
    do response.ContentEncoding <- Encoding.UTF8

    let toViewData = Map >> Some

    interface IAutoDisposable with
        member r.IsDisposed with get() = isDisposed
        member r.Dispose() =
            let headers = response.Headers
            [
                "Cache-Control", "no-cache, no-store, must-revalidate"
                "Pragma", "no-cache"
                "Expires", "0"
            ] |> List.iter(fun (k, v) -> if headers.[k] = null then headers.Add(k, v))
            if r.AddCharsetToHeader then
                response.Headers
                |> Headers.addCharset response
            writer.Dispose()
            isDisposed <- true

    member val AddCharsetToHeader = true with get, set
    member r.Response = response
    member r.Writer : StreamWriter = writer
    member r.ViewData with get() = viewData
    member private r.View(path : string option, viewData : ViewData option) =
        match view with
        | null -> failwith "View engine not found"
        | _ ->
            let viewData = r.ViewData |> combineViewData viewData
            let parameters = request.Params |> Map.map(fun _ v -> v |> Url.toPascalCase)
            view.Render(parameters, path, Some viewData) |> r.Writer.Write
    member r.View(path : string, ?viewData : ViewData) = r.View(Some path, viewData)
    member r.View(?viewData : ViewData) = r.View(None, viewData)
    member r.View(path : string, viewData : (string * obj) list) =
        r.View(Some path, viewData |> toViewData)
    member r.View(viewData : (string * obj) list) =
        r.View(None, viewData |> toViewData)
    member this.StatusCode with get() = response.StatusCode and set(value) = response.StatusCode <- value
    member this.ContentEncoding with get() = response.ContentEncoding and set(value) = response.ContentEncoding <- value
    member this.ContentType with get() = response.ContentType and set(value) = response.ContentType <- value
    member r.Redirect(url : string) = response.Redirect(url)
    member r.Refresh() = response.Redirect(request.RawUrl)
    member r.Back() = response.Redirect(request.UrlReferrer.ToString())
    member r.End(?statusCode : int) =
        match statusCode with
        | Some statusCode -> r.StatusCode <- statusCode
        | _ -> ()
        (r :> IAutoDisposable).Dispose()
    member r.SendFile(path : string, ?fileName : string, ?bufferSize : int) = async {
        let path = 
            [ Path.join([staticContentOptions.Directory; path]); path ]
            |> List.find(File.Exists)
        let fileName = defaultArg fileName (Path.GetFileName path)
        [
            "Content-Disposition", (sprintf "attachment; filename=\"%s\"" fileName)
            "Content-Transfer-Encoding", "binary"
            "Expires", "0"
            "Cache-Control", "must-revalidate, post-check=0, pre-check=0"
            "Pragma", "public"
        ] |> List.iter(fun (k, v) -> response.Headers.Add(k, v))
        do! File.sendBinary(path, response, {
            ContentType = "application/octet-stream"
            BufferSize = defaultArg bufferSize staticContentOptions.BufferSize
        }) }
    member r.Content(html : string, contentType : string) =
        r.ContentType <- contentType
        r.Writer.Write(html)
    member r.Html(html : string) =
        r.ContentType <- ContentTypes.html
        r.Writer.Write(html)
    member r.Text(text : string) =
        r.ContentType <- ContentTypes.plainText
        r.Writer.Write(text)
    member r.Xml(xml : string) =
        r.ContentType <- ContentTypes.xml
        r.Writer.Write(xml)
    member r.Json(json : string) =
        r.ContentType <- ContentTypes.json
        r.Writer.Write(json)

    member r.AsJson(o : obj) = 
        JSON.stringify(o) |> r.Json
    new(request : Request, response : Response, staticContentOptions : StaticContentOptions) =
        new Reply(request, response, staticContentOptions, null)
    new(request : Request, response : Response, staticContentConfig : StaticContentConfig, view : IView) =
        new Reply(request, response, staticContentConfig.GetOptions(), view)
    new(request : Request, response : Response, staticContentConfig : StaticContentConfig) =
        new Reply(request, response, staticContentConfig.GetOptions(), null)
    static member printf format = Printf.ksprintf printToReply format
    static member printfn format = Printf.ksprintf printToReplyLineEnd format